// Copyright (c) Microsoft. All rights reserved.

// ReSharper disable InconsistentNaming

/* launchSettings.json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "DeleteVectors": {
      "commandName": "Project",
      "environmentVariables": {
        "SEARCH_ENDPOINT": "...",
        "SEARCH_KEY": "..."
      }
    }
  }
}
*/

using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticMemory.Client.Models;

public class Program
{
    // Azure Search Index name
    private const string IndexName = "test01";

    // A Memory ID example. This value is later serialized.
    private const string ExternalRecordId = "usr=user2//ppl=f05//prt=7b9bad8968804121bb9b1264104608ac";

    // Size of the vectors
    private const int EmbeddingSize = 3;

    private static SearchIndexClient adminClient = null!;
    private static readonly string Endpoint = Environment.GetEnvironmentVariable("SEARCH_ENDPOINT")!;
    private static readonly string APIKey = Environment.GetEnvironmentVariable("SEARCH_KEY")!;

    public static async Task Main(string[] args)
    {
        // Azure Cognitive Search service client
        adminClient = new SearchIndexClient(new Uri(Endpoint), new AzureKeyCredential(APIKey),
            new SearchClientOptions { Diagnostics = { IsTelemetryEnabled = true, ApplicationId = "SemanticMemory" } });

        // Create an index (if doesn't exist)
        await CreateIndexAsync(IndexName);

        // Insert a record
        var recordId = await InsertRecordAsync(IndexName,
            ExternalRecordId,
            new Dictionary<string, object> { { "text", "a b c" } },
            new TagCollection(),
            new Embedding<float>(new[] { 0f, 0.5f, 1 }));

        // Delete the record
        await DeleteRecordAsync(IndexName,
            recordId,
            ExternalRecordId,
            new Dictionary<string, object> { { "text", "a b c" } },
            new TagCollection(),
            new Embedding<float>(new[] { 0f, 0.5f, 1 }));
    }

    // ===============================================================================================
    private static async Task CreateIndexAsync(string name)
    {
        Console.WriteLine("\n== CREATE INDEX ==\n");

        const string VectorSearchConfigName = "SemanticMemoryDefaultCosine";

        var indexSchema = new SearchIndex(name)
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

        indexSchema.Fields.Add(new SearchField("id", SearchFieldDataType.String)
        {
            IsKey = true,
            IsFilterable = true,
            IsFacetable = false,
            IsSortable = false,
            IsSearchable = true,
        });

        indexSchema.Fields.Add(new SimpleField("tags", SearchFieldDataType.Collection(SearchFieldDataType.String))
        {
            IsKey = false,
            IsFilterable = true,
            IsFacetable = false,
            IsSortable = false,
        });

        indexSchema.Fields.Add(new SearchField("metadata", SearchFieldDataType.String)
        {
            IsKey = false,
            IsFilterable = true,
            IsFacetable = false,
            IsSortable = false,
            IsSearchable = true,
        });

        indexSchema.Fields.Add(new SearchField("embedding", SearchFieldDataType.Collection(SearchFieldDataType.Single))
        {
            IsKey = false,
            IsFilterable = false,
            IsSearchable = true,
            IsFacetable = false,
            IsSortable = false,
            VectorSearchDimensions = EmbeddingSize,
            VectorSearchConfiguration = VectorSearchConfigName,
        });

        try
        {
            await adminClient.CreateIndexAsync(indexSchema);
        }
        catch (RequestFailedException e) when (e.Message.Contains("already exists"))
        {
            Console.WriteLine("Index already exists");
        }
    }

    // ===============================================================================================
    private static async Task<string> InsertRecordAsync(string indexName,
        string externalId, Dictionary<string, object> metadata, TagCollection tags, Embedding<float> embedding)
    {
        Console.WriteLine("\n== INSERT ==\n");
        var client = adminClient.GetSearchClient(indexName);

        var record = new Microsoft.SemanticMemory.Core.MemoryStorage.MemoryRecord
        {
            Id = externalId,
            Vector = embedding,
            Owner = "userAB",
            Tags = tags,
            Metadata = metadata
        };

        Microsoft.SemanticMemory.Core.MemoryStorage.AzureCognitiveSearch.AzureCognitiveSearchMemoryRecord localRecord = Microsoft.SemanticMemory.Core.MemoryStorage.AzureCognitiveSearch.AzureCognitiveSearchMemoryRecord.FromMemoryRecord(record);

        Console.WriteLine($"CREATING {localRecord.Id}\n");

        var response = await client.IndexDocumentsAsync(
            IndexDocumentsBatch.Upload(new[] { localRecord }),
            new IndexDocumentsOptions { ThrowOnAnyError = true });

        Console.WriteLine("Status: " + response.Value.Results.FirstOrDefault()?.Status);
        Console.WriteLine("Key: " + response.Value.Results.FirstOrDefault()?.Key);
        Console.WriteLine("Succeeded: " + (response.Value.Results.FirstOrDefault()?.Succeeded ?? false ? "true" : "false"));
        Console.WriteLine("ErrorMessage: " + response.Value.Results.FirstOrDefault()?.ErrorMessage);
        Console.WriteLine("Status: " + response.GetRawResponse().Status);
        Console.WriteLine("Content: " + response.GetRawResponse().Content);

        return response.Value.Results.FirstOrDefault()?.Key ?? string.Empty;
    }

    // ===============================================================================================
    private static async Task DeleteRecordAsync(string indexName,
        string recordId, string externalId, Dictionary<string, object> metadata, TagCollection tags, Embedding<float> embedding)
    {
        Console.WriteLine("\n== DELETE ==\n");

        var client = adminClient.GetSearchClient(indexName);

        Console.WriteLine($"DELETING {recordId}\n");

        Response<IndexDocumentsResult>? response = await client.DeleteDocumentsAsync("id", new List<string> { recordId });

        Console.WriteLine("Status: " + response.Value.Results.FirstOrDefault()?.Status);
        Console.WriteLine("Key: " + response.Value.Results.FirstOrDefault()?.Key);
        Console.WriteLine("Succeeded: " + (response.Value.Results.FirstOrDefault()?.Succeeded ?? false ? "true" : "false"));
        Console.WriteLine("ErrorMessage: " + response.Value.Results.FirstOrDefault()?.ErrorMessage);
        Console.WriteLine("Status: " + response.GetRawResponse().Status);
        Console.WriteLine("Content: " + response.GetRawResponse().Content);
    }
}
