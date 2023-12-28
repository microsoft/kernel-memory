// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.ContentStorage;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryStorage;

namespace Microsoft.KernelMemory.MemoryDb.AzureAISearch;

/// <summary>
/// Azure AI Search connector for Kernel Memory
/// TODO:
/// * support semantic search
/// * support hybrid search
/// * support custom schema
/// * support custom Azure AI Search logic
/// </summary>
public class AzureAISearchMemory : IMemoryDb
{
    private readonly ITextEmbeddingGenerator _embeddingGenerator;
    private readonly ILogger<AzureAISearchMemory> _log;

    /// <summary>
    /// Create a new instance
    /// </summary>
    /// <param name="config">Azure AI Search configuration</param>
    /// <param name="embeddingGenerator">Text embedding generator</param>
    /// <param name="log">Application logger</param>
    public AzureAISearchMemory(
        AzureAISearchConfig config,
        ITextEmbeddingGenerator embeddingGenerator,
        ILogger<AzureAISearchMemory>? log = null)
    {
        this._embeddingGenerator = embeddingGenerator;
        this._log = log ?? DefaultLogger<AzureAISearchMemory>.Instance;

        if (string.IsNullOrEmpty(config.Endpoint))
        {
            this._log.LogCritical("Azure AI Search Endpoint is empty");
            throw new ConfigurationException("Azure AI Search Endpoint is empty");
        }

        if (this._embeddingGenerator == null)
        {
            throw new AzureAISearchMemoryException("Embedding generator not configured");
        }

        switch (config.Auth)
        {
            case AzureAISearchConfig.AuthTypes.AzureIdentity:
                this._adminClient = new SearchIndexClient(
                    new Uri(config.Endpoint),
                    new DefaultAzureCredential(),
                    GetClientOptions());
                break;

            case AzureAISearchConfig.AuthTypes.APIKey:
                if (string.IsNullOrEmpty(config.APIKey))
                {
                    this._log.LogCritical("Azure AI Search API key is empty");
                    throw new ConfigurationException("Azure AI Search API key is empty");
                }

                this._adminClient = new SearchIndexClient(
                    new Uri(config.Endpoint),
                    new AzureKeyCredential(config.APIKey),
                    GetClientOptions());
                break;

            case AzureAISearchConfig.AuthTypes.ManualTokenCredential:
                this._adminClient = new SearchIndexClient(
                    new Uri(config.Endpoint),
                    config.GetTokenCredential(),
                    GetClientOptions());
                break;

            default:
                this._log.LogCritical("Azure AI Search authentication type '{0}' undefined or not supported", config.Auth);
                throw new ContentStorageException($"Azure AI Search authentication type '{config.Auth}' undefined or not supported");
        }
    }

    /// <inheritdoc />
    public Task CreateIndexAsync(string index, int vectorSize, CancellationToken cancellationToken = default)
    {
        return this.CreateIndexAsync(index, AzureAISearchMemoryRecord.GetSchema(vectorSize), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetIndexesAsync(CancellationToken cancellationToken = default)
    {
        var indexesAsync = this._adminClient.GetIndexesAsync(cancellationToken).ConfigureAwait(false);
        var result = new List<string>();
        await foreach (SearchIndex? index in indexesAsync.ConfigureAwait(false))
        {
            result.Add(index.Name);
        }

        return result;
    }

    /// <inheritdoc />
    public Task DeleteIndexAsync(string index, CancellationToken cancellationToken = default)
    {
        index = this.NormalizeIndexName(index);
        if (string.Equals(index, Constants.DefaultIndex, StringComparison.OrdinalIgnoreCase))
        {
            this._log.LogWarning("The default index cannot be deleted");
            return Task.CompletedTask;
        }

        return this._adminClient.DeleteIndexAsync(index, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> UpsertAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        var client = this.GetSearchClient(index);
        AzureAISearchMemoryRecord localRecord = AzureAISearchMemoryRecord.FromMemoryRecord(record);

        await client.IndexDocumentsAsync(
            IndexDocumentsBatch.Upload(new[] { localRecord }),
            new IndexDocumentsOptions { ThrowOnAnyError = true },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return record.Id;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<(MemoryRecord, double)> GetSimilarListAsync(
        string index,
        string text,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        int limit = 1,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (limit <= 0) { limit = int.MaxValue; }

        var client = this.GetSearchClient(index);

        Embedding textEmbedding = await this._embeddingGenerator.GenerateEmbeddingAsync(text, cancellationToken).ConfigureAwait(false);
        VectorizedQuery vectorQuery = new(textEmbedding.Data)
        {
            KNearestNeighborsCount = limit,
            Fields = { AzureAISearchMemoryRecord.VectorField },
            // Exhaustive search is a brute force comparison across all vectors,
            // ignoring the index, which can be much slower once the index contains a lot of data.
            // TODO: allow clients to manage this value either at configuration or run time.
            Exhaustive = false
        };

        SearchOptions options = new()
        {
            VectorSearch = new()
            {
                Queries = { vectorQuery },
                // Default, applies the vector query AFTER the search filter
                FilterMode = VectorFilterMode.PreFilter
            }
        };

        // Remove empty filters
        filters = filters?.Where(f => !f.IsEmpty()).ToList();

        if (filters is { Count: > 0 })
        {
            options.Filter = BuildSearchFilter(filters);
            options.Size = limit;

            this._log.LogDebug("Filtering vectors, limit {0}, condition: {1}", options.Size, options.Filter);
        }

        Response<SearchResults<AzureAISearchMemoryRecord>>? searchResult = null;
        try
        {
            searchResult = await client
                .SearchAsync<AzureAISearchMemoryRecord>(null, options, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (RequestFailedException e) when (e.Status == 404)
        {
            this._log.LogWarning("Not found: {0}", e.Message);
            // Index not found, no data to return
        }

        if (searchResult == null) { yield break; }

        var minDistance = CosineSimilarityToScore(minRelevance);
        await foreach (SearchResult<AzureAISearchMemoryRecord>? doc in searchResult.Value.GetResultsAsync().ConfigureAwait(false))
        {
            if (doc == null || doc.Score < minDistance) { continue; }

            MemoryRecord memoryRecord = doc.Document.ToMemoryRecord(withEmbeddings);

            yield return (memoryRecord, ScoreToCosineSimilarity(doc.Score ?? 0));
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<MemoryRecord> GetListAsync(
        string index,
        ICollection<MemoryFilter>? filters = null,
        int limit = 1,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (limit <= 0) { limit = int.MaxValue; }

        var client = this.GetSearchClient(index);

        // Remove empty filters
        filters = filters?.Where(f => !f.IsEmpty()).ToList();

        SearchOptions options = new();
        if (filters is { Count: > 0 })
        {
            options.Filter = BuildSearchFilter(filters);
            options.Size = limit;

            this._log.LogDebug("Filtering vectors, limit {0}, condition: {1}", options.Size, options.Filter);
        }

        // See: https://learn.microsoft.com/azure/search/search-query-understand-collection-filters
        // fieldValue = fieldValue.Replace("'", "''", StringComparison.Ordinal);
        // var options = new SearchOptions
        // {
        //     Filter = fieldIsCollection
        //         ? $"{fieldName}/any(s: s eq '{fieldValue}')"
        //         : $"{fieldName} eq '{fieldValue}')",
        //     Size = limit
        // };

        Response<SearchResults<AzureAISearchMemoryRecord>>? searchResult = null;
        try
        {
            searchResult = await client
                .SearchAsync<AzureAISearchMemoryRecord>(null, options, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (RequestFailedException e) when (e.Status == 404)
        {
            this._log.LogWarning("Not found: {0}", e.Message);
            // Index not found, no data to return
        }

        if (searchResult == null) { yield break; }

        await foreach (SearchResult<AzureAISearchMemoryRecord>? doc in searchResult.Value.GetResultsAsync().ConfigureAwait(false))
        {
            // stop after returning the amount requested
            if (limit-- <= 0) { yield break; }

            yield return doc.Document.ToMemoryRecord(withEmbeddings);
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        string id = AzureAISearchMemoryRecord.FromMemoryRecord(record).Id;
        var client = this.GetSearchClient(index);

        try
        {
            this._log.LogDebug("Deleting record {0} from index {1}", id, index);
            Response<IndexDocumentsResult>? result = await client.DeleteDocumentsAsync(
                    AzureAISearchMemoryRecord.IdField,
                    new List<string> { id },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            this._log.LogTrace("Delete response status: {0}, content: {1}", result.GetRawResponse().Status, result.GetRawResponse().Content.ToString());
        }
        catch (RequestFailedException e) when (e.Status == 404)
        {
            this._log.LogTrace("Index {0} record {1} not found, nothing to delete", index, id);
        }
    }

    #region private

    // private async Task<AzureAISearchMemoryRecord?> GetAsync(string indexName, string id, CancellationToken cancellationToken = default)
    // {
    //     try
    //     {
    //         Response<AzureAISearchMemoryRecord>? result = await this.GetSearchClient(indexName)
    //             .GetDocumentAsync<AzureAISearchMemoryRecord>(id, cancellationToken: cancellationToken)
    //             .ConfigureAwait(false);
    //
    //         return result?.Value;
    //     }
    //     catch (Exception e)
    //     {
    //         this._log.LogError(e, "Failed to fetch record");
    //         return null;
    //     }
    // }

    private async Task CreateIndexAsync(string index, MemoryDbSchema schema, CancellationToken cancellationToken = default)
    {
        if (await this.DoesIndexExistAsync(index, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var indexSchema = this.PrepareIndexSchema(index, schema);

        try
        {
            await this._adminClient.CreateIndexAsync(indexSchema, cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException e) when (e.Status == 409)
        {
            this._log.LogWarning("Index already exists, nothing to do: {0}", e.Message);
        }
    }

    private async Task<bool> DoesIndexExistAsync(string index, CancellationToken cancellationToken = default)
    {
        string normalizeIndexName = this.NormalizeIndexName(index);

        var indexesAsync = this._adminClient.GetIndexesAsync(cancellationToken).ConfigureAwait(false);
        await foreach (SearchIndex? searchIndex in indexesAsync.ConfigureAwait(false))
        {
            if (searchIndex != null && string.Equals(searchIndex.Name, normalizeIndexName, StringComparison.OrdinalIgnoreCase)) { return true; }
        }

        return false;
    }

    private async IAsyncEnumerable<string> UpsertBatchAsync(
        string index,
        IEnumerable<MemoryRecord> records,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var client = this.GetSearchClient(index);

        foreach (MemoryRecord record in records)
        {
            var localRecord = AzureAISearchMemoryRecord.FromMemoryRecord(record);
            await client.IndexDocumentsAsync(
                IndexDocumentsBatch.Upload(new[] { localRecord }),
                new IndexDocumentsOptions { ThrowOnAnyError = true },
                cancellationToken: cancellationToken).ConfigureAwait(false);

            yield return record.Id;
        }
    }

    /// <summary>
    /// Index names cannot contain special chars. We use this rule to replace a few common ones
    /// with an underscore and reduce the chance of errors. If other special chars are used, we leave it
    /// to the service to throw an error.
    /// Note:
    /// - replacing chars introduces a small chance of conflicts, e.g. "the-user" and "the_user".
    /// - we should consider whether making this optional and leave it to the developer to handle.
    /// </summary>
    private static readonly Regex s_replaceIndexNameCharsRegex = new(@"[\s|\\|/|.|_|:]");

    private readonly ConcurrentDictionary<string, SearchClient> _clientsByIndex = new();

    private readonly SearchIndexClient _adminClient;

    /// <summary>
    /// Get a search client for the index specified.
    /// Note: the index might not exist, but we avoid checking everytime and the extra latency.
    /// </summary>
    /// <param name="index">Index name</param>
    /// <returns>Search client ready to read/write</returns>
    private SearchClient GetSearchClient(string index)
    {
        var normalIndexName = this.NormalizeIndexName(index);
        this._log.LogTrace("Preparing search client, index name '{0}' normalized to '{1}'", index, normalIndexName);

        // Search an available client from the local cache
        if (!this._clientsByIndex.TryGetValue(normalIndexName, out SearchClient? client))
        {
            client = this._adminClient.GetSearchClient(normalIndexName);
            this._clientsByIndex[normalIndexName] = client;
        }

        return client;
    }

    private static void ValidateSchema(MemoryDbSchema schema)
    {
        schema.Validate(vectorSizeRequired: true);

        foreach (var f in schema.Fields.Where(x => x.Type == MemoryDbField.FieldType.Vector))
        {
            if (f.VectorMetric is not (MemoryDbField.VectorMetricType.Cosine or MemoryDbField.VectorMetricType.Euclidean or MemoryDbField.VectorMetricType.DotProduct))
            {
                throw new AzureAISearchMemoryException($"Vector metric '{f.VectorMetric:G}' not supported");
            }
        }
    }

    /// <summary>
    /// Options used by the Azure AI Search client, e.g. User Agent.
    /// See also https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/core/Azure.Core/src/DiagnosticsOptions.cs
    /// </summary>
    private static SearchClientOptions GetClientOptions()
    {
        return new SearchClientOptions
        {
            Diagnostics =
            {
                IsTelemetryEnabled = Telemetry.IsTelemetryEnabled,
                ApplicationId = Telemetry.HttpUserAgent,
            },
        };
    }

    /// <summary>
    /// Normalize index name to match Azure AI Search rules.
    /// The method doesn't handle all the error scenarios, leaving it to the service
    /// to throw an error for edge cases not handled locally.
    /// </summary>
    /// <param name="index">Value to normalize</param>
    /// <returns>Normalized name</returns>
    private string NormalizeIndexName(string index)
    {
        if (string.IsNullOrWhiteSpace(index))
        {
            index = Constants.DefaultIndex;
        }

        if (index.Length > 128)
        {
            throw new AzureAISearchMemoryException("The index name (prefix included) is too long, it cannot exceed 128 chars.");
        }

        index = index.ToLowerInvariant();

        index = s_replaceIndexNameCharsRegex.Replace(index.Trim(), "-");

        // Name cannot start with a dash
        if (index.StartsWith('-')) { index = $"z{index}"; }

        // Name cannot end with a dash
        if (index.EndsWith('-')) { index = $"{index}z"; }

        return index;
    }

    private SearchIndex PrepareIndexSchema(string index, MemoryDbSchema schema)
    {
        ValidateSchema(schema);

        index = this.NormalizeIndexName(index);

        const string VectorSearchProfileName = "KMDefaultProfile";
        const string VectorSearchConfigName = "KMDefaultAlgorithm";

        var indexSchema = new SearchIndex(index)
        {
            Fields = new List<SearchField>(),
            VectorSearch = new VectorSearch
            {
                Profiles =
                {
                    new VectorSearchProfile(VectorSearchProfileName, VectorSearchConfigName)
                },
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(VectorSearchConfigName)
                    {
                        Parameters = new HnswParameters
                        {
                            Metric = VectorSearchAlgorithmMetric.Cosine
                        }
                    }
                }
            }
        };

        /* Field attributes: see https://learn.microsoft.com/en-us/azure/search/search-what-is-an-index
         * - searchable: Full-text searchable, subject to lexical analysis such as word-breaking during indexing.
         * - filterable: Filterable fields of type Edm.String or Collection(Edm.String) don't undergo word-breaking.
         * - facetable: Used for counting. Fields of type Edm.String that are filterable, "sortable", or "facetable" can be at most 32kb. */
        SearchField? vectorField = null;
        foreach (var field in schema.Fields)
        {
            switch (field.Type)
            {
                case MemoryDbField.FieldType.Unknown:
                default:
                    throw new AzureAISearchMemoryException($"Unsupported field type {field.Type:G}");

                case MemoryDbField.FieldType.Vector:
                    vectorField = new SearchField(field.Name, SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsKey = false,
                        IsFilterable = false,
                        IsSearchable = true,
                        IsFacetable = false,
                        IsSortable = false,
                        VectorSearchDimensions = field.VectorSize,
                        VectorSearchProfileName = VectorSearchProfileName,
                    };

                    break;
                case MemoryDbField.FieldType.Text:
                    var useBugWorkAround = true;
                    if (useBugWorkAround)
                    {
                        /* August 2023:
                           - bug: Indexes must have a searchable string field
                           - temporary workaround: make the key field searchable

                         Example of unexpected error:
                            Date: Tue, 01 Aug 2023 23:15:59 GMT
                            Status: 400 (Bad Request)
                            ErrorCode: OperationNotAllowed

                            Content:
                            {"error":{"code":"OperationNotAllowed","message":"If a query contains the search option the
                            target index must contain one or more searchable string fields.\r\nParameter name: search",
                            "details":[{"code":"CannotSearchWithoutSearchableFields","message":"If a query contains the
                            search option the target index must contain one or more searchable string fields."}]}}

                            at Azure.Search.Documents.SearchClient.SearchInternal[T](SearchOptions options,
                            String operationName, Boolean async, CancellationToken cancellationToken)
                         */
                        indexSchema.Fields.Add(new SearchField(field.Name, SearchFieldDataType.String)
                        {
                            IsKey = field.IsKey,
                            IsFilterable = field.IsKey || field.IsFilterable, // Filterable keys are recommended for batch operations
                            IsFacetable = false,
                            IsSortable = false,
                            IsSearchable = true,
                        });
                    }
                    else
                    {
                        indexSchema.Fields.Add(new SimpleField(field.Name, SearchFieldDataType.String)
                        {
                            IsKey = field.IsKey,
                            IsFilterable = field.IsKey || field.IsFilterable, // Filterable keys are recommended for batch operations
                            IsFacetable = false,
                            IsSortable = false,
                        });
                    }

                    break;

                case MemoryDbField.FieldType.Integer:
                    indexSchema.Fields.Add(new SimpleField(field.Name, SearchFieldDataType.Int64)
                    {
                        IsKey = field.IsKey,
                        IsFilterable = field.IsKey || field.IsFilterable, // Filterable keys are recommended for batch operations
                        IsFacetable = false,
                        IsSortable = false,
                    });
                    break;

                case MemoryDbField.FieldType.Decimal:
                    indexSchema.Fields.Add(new SimpleField(field.Name, SearchFieldDataType.Double)
                    {
                        IsKey = field.IsKey,
                        IsFilterable = field.IsKey || field.IsFilterable, // Filterable keys are recommended for batch operations
                        IsFacetable = false,
                        IsSortable = false,
                    });
                    break;

                case MemoryDbField.FieldType.Bool:
                    indexSchema.Fields.Add(new SimpleField(field.Name, SearchFieldDataType.Boolean)
                    {
                        IsKey = false,
                        IsFilterable = field.IsFilterable,
                        IsFacetable = false,
                        IsSortable = false,
                    });
                    break;

                case MemoryDbField.FieldType.ListOfStrings:
                    indexSchema.Fields.Add(new SimpleField(field.Name, SearchFieldDataType.Collection(SearchFieldDataType.String))
                    {
                        IsKey = false,
                        IsFilterable = field.IsFilterable,
                        IsFacetable = false,
                        IsSortable = false,
                    });
                    break;
            }
        }

        // Add the vector field as the last element, so Azure Portal shows
        // the other fields before the long list of floating numbers
        indexSchema.Fields.Add(vectorField);

        return indexSchema;
    }

    private static double ScoreToCosineSimilarity(double score)
    {
        return 2 - 1 / score;
    }

    private static double CosineSimilarityToScore(double similarity)
    {
        return 1 / (2 - similarity);
    }

    private static string BuildSearchFilter(IEnumerable<MemoryFilter> filters)
    {
        List<string> conditions = new();

        // Note: empty filters would lead to a syntax error, so even if they are supposed
        // to be removed upstream, we check again and remove them here too.
        foreach (var filter in filters.Where(f => !f.IsEmpty()))
        {
            var filterConditions = filter.GetFilters()
                .Select(keyValue =>
                {
                    var fieldValue = keyValue.Value?.Replace("'", "''", StringComparison.Ordinal);
                    return $"tags/any(s: s eq '{keyValue.Key}{Constants.ReservedEqualsChar}{fieldValue}')";
                })
                .ToList();

            conditions.Add($"({string.Join(" and ", filterConditions)})");
        }

        // Examples:
        // (tags/any(s: s eq 'user:someone1') and tags/any(s: s eq 'type:news')) or (tags/any(s: s eq 'user:someone2') and tags/any(s: s eq 'type:news'))
        // (tags/any(s: s eq 'user:someone1') and tags/any(s: s eq 'type:news')) or (tags/any(s: s eq 'user:admin') and tags/any(s: s eq 'type:fact'))
        return string.Join(" or ", conditions);
    }

    #endregion
}
