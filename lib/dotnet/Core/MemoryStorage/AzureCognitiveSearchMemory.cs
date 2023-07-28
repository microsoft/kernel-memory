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
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.Diagnostics;

namespace Microsoft.SemanticMemory.Core.MemoryStorage;

public class AzureCognitiveSearchMemory
{
    public AzureCognitiveSearchMemory(string endpoint, string apiKey)
    {
        if (string.IsNullOrEmpty(endpoint))
        {
            throw new ConfigurationException("Azure Cognitive Search Endpoint is empty");
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ConfigurationException("Azure Cognitive Search API key is empty");
        }

        AzureKeyCredential credentials = new(apiKey);
        this._adminClient = new SearchIndexClient(new Uri(endpoint), credentials, GetClientOptions());
    }

    public async Task<bool> DoesCollectionExistAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        string indexName = NormalizeIndexName(collectionName);

        return await this.GetIndexesAsync(cancellationToken)
            .AnyAsync(index => string.Equals(index, indexName, StringComparison.OrdinalIgnoreCase),
                cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task CreateCollectionAsync(string collectionName, VectorDbSchema schema, CancellationToken cancellationToken = default)
    {
        if (await this.DoesCollectionExistAsync(collectionName, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var indexSchema = PrepareIndexSchema(collectionName, schema);
        await this._adminClient.CreateIndexAsync(indexSchema, cancellationToken).ConfigureAwait(false);
    }

    public Task DeleteCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        return this._adminClient.DeleteIndexAsync(NormalizeIndexName(collectionName), cancellationToken);
    }

    public async Task<string> UpsertAsync(string collectionName, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        var client = this.GetSearchClient(collectionName);
        AzureCognitiveSearchMemoryRecord localRecord = AzureCognitiveSearchMemoryRecord.FromMemoryRecord(record);

        await client.IndexDocumentsAsync(
            IndexDocumentsBatch.Upload(new[] { localRecord }),
            new IndexDocumentsOptions { ThrowOnAnyError = true },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return record.Id;
    }

    public async IAsyncEnumerable<string> UpsertBatchAsync(string collectionName, IEnumerable<MemoryRecord> records, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var client = this.GetSearchClient(collectionName);

        foreach (MemoryRecord record in records)
        {
            var localRecord = AzureCognitiveSearchMemoryRecord.FromMemoryRecord(record);
            await client.IndexDocumentsAsync(
                IndexDocumentsBatch.Upload(new[] { localRecord }),
                new IndexDocumentsOptions { ThrowOnAnyError = true },
                cancellationToken: cancellationToken).ConfigureAwait(false);

            yield return record.Id;
        }
    }

    #region private

    /// <summary>
    /// Index names cannot contain special chars. We use this rule to replace a few common ones
    /// with an underscore and reduce the chance of errors. If other special chars are used, we leave it
    /// to the service to throw an error.
    /// Note:
    /// - replacing chars introduces a small chance of conflicts, e.g. "the-user" and "the_user".
    /// - we should consider whether making this optional and leave it to the developer to handle.
    /// </summary>
    private static readonly Regex s_replaceIndexNameSymbolsRegex = new(@"[\s|\\|/|.|_|:]");

    private readonly ConcurrentDictionary<string, SearchClient> _clientsByIndex = new();

    private readonly SearchIndexClient _adminClient;

    /// <summary>
    /// Get a search client for the index specified.
    /// Note: the index might not exist, but we avoid checking everytime and the extra latency.
    /// </summary>
    /// <param name="indexName">Index name</param>
    /// <returns>Search client ready to read/write</returns>
    private SearchClient GetSearchClient(string indexName)
    {
        indexName = NormalizeIndexName(indexName);

        // Search an available client from the local cache
        if (!this._clientsByIndex.TryGetValue(indexName, out SearchClient? client))
        {
            client = this._adminClient.GetSearchClient(indexName);
            this._clientsByIndex[indexName] = client;
        }

        return client;
    }

    private async IAsyncEnumerable<string> GetIndexesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var indexesAsync = this._adminClient.GetIndexesAsync(cancellationToken).ConfigureAwait(false);
        await foreach (SearchIndex? index in indexesAsync)
        {
            yield return index.Name;
        }
    }

    private static void ValidateSchema(VectorDbSchema schema)
    {
        schema.Validate(vectorSizeRequired: true);

        foreach (var f in schema.Fields.Where(x => x.Type == VectorDbField.FieldType.Vector))
        {
            if (f.VectorMetric is not (VectorDbField.VectorMetricType.Cosine or VectorDbField.VectorMetricType.Euclidean or VectorDbField.VectorMetricType.DotProduct))
            {
                throw new AzureCognitiveSearchMemoryException($"Vector metric '{f.VectorMetric:G}' not supported");
            }
        }
    }

    /// <summary>
    /// Options used by the Azure Cognitive Search client, e.g. User Agent.
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
    /// Normalize index name to match ACS rules.
    /// The method doesn't handle all the error scenarios, leaving it to the service
    /// to throw an error for edge cases not handled locally.
    /// </summary>
    /// <param name="indexName">Value to normalize</param>
    /// <returns>Normalized name</returns>
    private static string NormalizeIndexName(string indexName)
    {
        if (indexName.Length > 128)
        {
            throw new AzureCognitiveSearchMemoryException("The collection name is too long, it cannot exceed 128 chars.");
        }

#pragma warning disable CA1308 // The service expects a lowercase string
        indexName = indexName.ToLowerInvariant();
#pragma warning restore CA1308

        return s_replaceIndexNameSymbolsRegex.Replace(indexName.Trim(), "-");
    }

    private static SearchIndex PrepareIndexSchema(string indexName, VectorDbSchema schema)
    {
        ValidateSchema(schema);

        indexName = NormalizeIndexName(indexName);

        const string VectorSearchConfigName = "SemanticMemoryDefaultCosine";

        var indexSchema = new SearchIndex(indexName)
        {
            Fields = new List<SearchField>(),
            VectorSearch = new VectorSearch
            {
                AlgorithmConfigurations =
                {
                    new HnswVectorSearchAlgorithmConfiguration(VectorSearchConfigName)
                    {
                        Parameters = new HnswParameters { Metric = VectorSearchAlgorithmMetric.Cosine }
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
                case VectorDbField.FieldType.Unknown:
                default:
                    throw new AzureCognitiveSearchMemoryException($"Unsupported field type {field.Type:G}");

                case VectorDbField.FieldType.Vector:

                    vectorField = new SearchField(field.Name, SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsKey = false,
                        IsFilterable = false,
                        IsSearchable = true,
                        IsFacetable = false,
                        IsSortable = false,
                        VectorSearchDimensions = field.VectorSize,
                        VectorSearchConfiguration = VectorSearchConfigName,
                    };

                    break;
                case VectorDbField.FieldType.Text:
                    indexSchema.Fields.Add(new SimpleField(field.Name, SearchFieldDataType.String)
                    {
                        IsKey = field.IsKey,
                        IsFilterable = field.IsKey || field.IsFilterable,
                        IsFacetable = false,
                        IsSortable = false,
                    });
                    break;

                case VectorDbField.FieldType.Integer:
                    indexSchema.Fields.Add(new SimpleField(field.Name, SearchFieldDataType.Int64)
                    {
                        IsKey = field.IsKey,
                        IsFilterable = field.IsKey || field.IsFilterable,
                        IsFacetable = false,
                        IsSortable = false,
                    });
                    break;

                case VectorDbField.FieldType.Decimal:
                    indexSchema.Fields.Add(new SimpleField(field.Name, SearchFieldDataType.Double)
                    {
                        IsKey = field.IsKey,
                        IsFilterable = field.IsKey || field.IsFilterable,
                        IsFacetable = false,
                        IsSortable = false,
                    });
                    break;

                case VectorDbField.FieldType.Bool:
                    indexSchema.Fields.Add(new SimpleField(field.Name, SearchFieldDataType.Boolean)
                    {
                        IsKey = false,
                        IsFilterable = field.IsFilterable,
                        IsFacetable = false,
                        IsSortable = false,
                    });
                    break;

                case VectorDbField.FieldType.ListOfStrings:
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

    #endregion
}
