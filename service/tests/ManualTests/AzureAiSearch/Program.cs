// Copyright (c) Microsoft. All rights reserved.

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
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.MemoryStorage.AzureAISearch;

namespace AzureAISearch;

public static class Program
{
    // Azure Search Index name
    private const string Index = "test01";

    // A Memory ID example. This value is later serialized.
    private const string ExternalRecordId1 = "usr=user2//ppl=f05//prt=7b9bad8968804121bb9b1264104608ac";
    private const string ExternalRecordId2 = "usr=user2//ppl=f06//prt=7b9bad8968804121bb9b1264104608ac";

    // Size of the vectors
    private const int EmbeddingSize = 3;

    private const string VectorSearchProfileName = "KMDefaultProfile";
    private const string VectorSearchConfigName = "KMDefaultAlgorithm";

    private static SearchIndexClient s_adminClient = null!;
    private static readonly string s_endpoint = Env.Var("SEARCH_ENDPOINT")!;
    private static readonly string s_apiKey = Env.Var("SEARCH_KEY")!;

    public static async Task RunAsync()
    {
        // Azure AI Search service client
        s_adminClient = new SearchIndexClient(
            new Uri(s_endpoint),
            new AzureKeyCredential(s_apiKey),
            new SearchClientOptions
            {
                Diagnostics =
                {
                    IsTelemetryEnabled = true,
                    ApplicationId = "Semantic-Kernel"
                }
            });

        // Create an index (if doesn't exist)
        await CreateIndexAsync(Index);

        // Insert two records
        var recordId1 = await InsertRecordAsync(Index,
            externalId: ExternalRecordId1,
            payload: new Dictionary<string, object> { { "filename", "dotnet.pdf" }, { "text", "this is a sentence" }, },
            tags: new TagCollection
            {
                { "category", "demo" },
                { "category", "dotnet" },
                { "category", "search" },
                { "year", "2024" },
            },
            embedding: new[] { 0f, 0.5f, 1 });

        var recordId2 = await InsertRecordAsync(Index,
            externalId: ExternalRecordId2,
            payload: new Dictionary<string, object> { { "filename", "python.pdf" }, { "text", "this is a sentence" }, },
            tags: new TagCollection
            {
                { "category", "demo" },
                { "category", "pyt'hon" },
                { "category", "search" },
                { "year", "2023" },
            },
            embedding: new[] { 0f, 0.5f, 1 });

        await Task.Delay(TimeSpan.FromSeconds(2));

        // Search by tags
        var records = await SearchByFieldValueAsync(Index,
            fieldName: "tags",
            fieldValue1: $"category{Constants.ReservedEqualsChar}pyt'hon",
            fieldValue2: $"year{Constants.ReservedEqualsChar}2023",
            fieldIsCollection: true,
            limit: 5);

        Console.WriteLine("Count: " + records.Count + $" ({(records.Count == 1 ? "OK" : "ERROR, should be 1")})");
        foreach (MemoryRecord rec in records)
        {
            Console.WriteLine(" - " + rec.Id);
            Console.WriteLine("   " + rec.Payload.FirstOrDefault().Value);
        }

        // Search by tags
        records = await SearchByFieldValueAsync(Index,
            fieldName: "tags",
            fieldValue1: $"category{Constants.ReservedEqualsChar}pyt'hon",
            fieldValue2: $"year{Constants.ReservedEqualsChar}1999",
            fieldIsCollection: true,
            limit: 5);

        Console.WriteLine("Count: " + records.Count + $" ({(records.Count == 0 ? "OK" : "ERROR, should be 0")})");
        foreach (MemoryRecord rec in records)
        {
            Console.WriteLine(" - " + rec.Id);
            Console.WriteLine("   " + rec.Payload.FirstOrDefault().Value);
        }

        // Delete the record
        await DeleteRecordAsync(Index, recordId1);
        await DeleteRecordAsync(Index, recordId2);
    }

    // ===============================================================================================
    private static async Task CreateIndexAsync(string name)
    {
        Console.WriteLine("\n== CREATE INDEX ==\n");

        var indexSchema = new SearchIndex(name)
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

        indexSchema.Fields.Add(new SearchField("payload", SearchFieldDataType.String)
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
            VectorSearchProfileName = VectorSearchProfileName,
        });

        try
        {
            Response<SearchIndex>? response = await s_adminClient.CreateIndexAsync(indexSchema);

            Console.WriteLine("Status: " + response.GetRawResponse().Status);
            Console.WriteLine("IsError: " + response.GetRawResponse().IsError);
            Console.WriteLine("Content: " + response.GetRawResponse().Content);
            Console.WriteLine("Name: " + response.Value.Name);
        }
        catch (RequestFailedException e) when (e.Message.Contains("already exists"))
        {
            Console.WriteLine("Index already exists");
        }
    }

    // ===============================================================================================
    private static async Task<string> InsertRecordAsync(string index,
        string externalId, Dictionary<string, object> payload, TagCollection tags, Embedding embedding)
    {
        Console.WriteLine("\n== INSERT ==\n");
        var client = s_adminClient.GetSearchClient(index);

        var record = new MemoryRecord
        {
            Id = externalId,
            Vector = embedding,
            // Owner = "userAB",
            Tags = tags,
            Payload = payload
        };

        AzureAISearchMemoryRecord localRecord = AzureAISearchMemoryRecord.FromMemoryRecord(record);

        Console.WriteLine($"CREATING {localRecord.Id}\n");

        var response = await client.IndexDocumentsAsync(
            IndexDocumentsBatch.Upload(new[] { localRecord }),
            new IndexDocumentsOptions { ThrowOnAnyError = true });

        Console.WriteLine("Status: " + response.GetRawResponse().Status);
        Console.WriteLine("IsError: " + response.GetRawResponse().IsError);
        Console.WriteLine("Content: " + response.GetRawResponse().Content);

        Console.WriteLine("[Results] Status: " + response.Value.Results.FirstOrDefault()?.Status);
        Console.WriteLine("[Results] Key: " + response.Value.Results.FirstOrDefault()?.Key);
        Console.WriteLine("[Results] Succeeded: " + (response.Value.Results.FirstOrDefault()?.Succeeded ?? false ? "true" : "false"));
        Console.WriteLine("[Results] ErrorMessage: " + response.Value.Results.FirstOrDefault()?.ErrorMessage);

        return response.Value.Results.FirstOrDefault()?.Key ?? string.Empty;
    }

    // ===============================================================================================
    private static async Task<IList<MemoryRecord>> SearchByFieldValueAsync(
        string index,
        string fieldName,
        bool fieldIsCollection,
        string fieldValue1,
        string fieldValue2,
        int limit)
    {
        Console.WriteLine("\n== FILTER SEARCH ==\n");
        var client = s_adminClient.GetSearchClient(index);

        fieldValue1 = fieldValue1.Replace("'", "''", StringComparison.Ordinal);
        fieldValue2 = fieldValue2.Replace("'", "''", StringComparison.Ordinal);
        SearchOptions options = new()
        {
            Filter = fieldIsCollection
                ? $"{fieldName}/any(s: s eq '{fieldValue1}') and {fieldName}/any(s: s eq '{fieldValue2}')"
                : $"{fieldName} eq '{fieldValue1}' or {fieldName} eq '{fieldValue2}')",
            Size = limit
        };

        Response<SearchResults<AzureAISearchMemoryRecord>>? searchResult = null;
        try
        {
            searchResult = await client.SearchAsync<AzureAISearchMemoryRecord>(null, options);
        }
        catch (RequestFailedException e) when (e.Status == 404)
        {
            Console.WriteLine("Search returned 404: {0}", e.Message);
        }

        var results = new List<MemoryRecord>();

        if (searchResult == null) { return results; }

        await foreach (SearchResult<AzureAISearchMemoryRecord>? doc in searchResult.Value.GetResultsAsync())
        {
            results.Add(doc.Document.ToMemoryRecord());
        }

        return results;
    }

    // ===============================================================================================
    private static async Task DeleteRecordAsync(string index, string recordId)
    {
        Console.WriteLine("\n== DELETE ==\n");

        var client = s_adminClient.GetSearchClient(index);

        Console.WriteLine($"DELETING {recordId}\n");

        Response<IndexDocumentsResult>? response = await client.DeleteDocumentsAsync("id", new List<string> { recordId });

        Console.WriteLine("Status: " + response.GetRawResponse().Status);
        Console.WriteLine("IsError: " + response.GetRawResponse().IsError);
        Console.WriteLine("Content: " + response.GetRawResponse().Content);

        Console.WriteLine("[Results] Status: " + response.Value.Results.FirstOrDefault()?.Status);
        Console.WriteLine("[Results] Key: " + response.Value.Results.FirstOrDefault()?.Key);
        Console.WriteLine("[Results] Succeeded: " + (response.Value.Results.FirstOrDefault()?.Succeeded ?? false ? "true" : "false"));
        Console.WriteLine("[Results] ErrorMessage: " + response.Value.Results.FirstOrDefault()?.ErrorMessage);
    }
}
