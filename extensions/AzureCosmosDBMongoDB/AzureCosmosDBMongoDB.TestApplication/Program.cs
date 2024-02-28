// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.MemoryDb.AzureCosmosDBMongoDB;
using Microsoft.KernelMemory.MemoryStorage;
using MongoDB.Driver;

namespace AzureCosmosDBMongoDB.TestApplication;

public static class Program
{
    // Azure Cosmos DB MongoDB vCore Database Name
    private const string DatabaseName = "cosmos_test_db";

    // Azure Cosmos DB MongoDB vCore Collection Name
    private const string CollectionName = "cosmos_test_collection";

    private const string Text1 = "this is test 1";
    private const string Text2 = "this is test 2";

    public static async Task Main()
    {

        var (memory, embeddings) = await SetupAsync();

        Console.WriteLine("++++ DELETE INDEX ++++");
        
        await memory.DeleteIndexAsync("default_index");

        Console.WriteLine("++++ CREATE INDEX ++++");

        await memory.CreateIndexAsync("default_index", embeddings[0].Length);

        Console.WriteLine("++++ LIST INDEXES ++++");

        IEnumerable<string> indexes = await memory.GetIndexesAsync();
        foreach (var indexName in indexes)
        {
            Console.WriteLine(indexName);
        }

        Console.WriteLine("===== INSERT RECORD 1 AND 2 =====");
        var memoryRecord1 = new MemoryRecord
        {
            Id = "memory 1",
            Vector = embeddings[0],
            Tags = new TagCollection { { "updated", "no" }, { "type", "email" } },
            Payload = new Dictionary<string, object>()
        };

        var memoryRecord2 = new MemoryRecord
        {
            Id = "memory 2",
            Vector = embeddings[0],
            Tags = new TagCollection { { "updated", "no" }, { "type", "email" } },
            Payload = new Dictionary<string, object>()
        };

        var id1 = await memory.UpsertAsync("default_index", memoryRecord1);
        Console.WriteLine($"Insert 1: {id1} {memoryRecord1.Id}");

        var id2 = await memory.UpsertAsync("default_index", memoryRecord2);
        Console.WriteLine($"Insert 2: {id2} {memoryRecord2.Id}");


        Console.WriteLine("===== INSERT RECORD 3 =====");

        var memoryRecord3 = new MemoryRecord
        {
            Id = "memory 3",
            Vector = embeddings[1],
            Tags = new TagCollection { { "type", "news" } },
            Payload = new Dictionary<string, object>()
        };

        var id3 = await memory.UpsertAsync("default_index", memoryRecord3);
        Console.WriteLine($"Insert 3: {id3} {memoryRecord3.Id}");

        Console.WriteLine("===== UPDATE RECORD 3 =====");

        memoryRecord3.Tags.Add("updated", "yes");
        id3 = await memory.UpsertAsync("default_index", memoryRecord3);
        Console.WriteLine($"Update 3: {id3} {memoryRecord3.Id}");

        Console.WriteLine("===== SEARCH 1 =====");

        var similarList = memory.GetSimilarListAsync(
            "default_index", text: Text1, limit: 10, withEmbeddings: true, minRelevance:0.7);
        await foreach((MemoryRecord, double) record in similarList)
        {
            Console.WriteLine(record.Item1.Id);
            Console.WriteLine("  score: " + record.Item2);
            Console.WriteLine("  tags: " + record.Item1.Tags.Count);
            Console.WriteLine("  size: " + record.Item1.Vector.Length);
        }    

        Console.WriteLine("===== DELETE =====");

        await memory.DeleteAsync("test", new MemoryRecord { Id = "memory 1" });
        await memory.DeleteAsync("test", new MemoryRecord { Id = "memory 2" });
        await memory.DeleteAsync("test", new MemoryRecord { Id = "memory 3" });
        
        Console.WriteLine("== Done ==");

    }

    private static async Task<(AzureCosmosDBMongoDBMemory, Embedding[])> SetupAsync()
    {
        IConfiguration cfg = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var config = cfg.GetSection("KernelMemory:Services:AzureCosmosDBMongoDB").Get<AzureCosmosDBMongoDBConfig>()
                     ?? throw new ArgumentNullException(message: "AzureAISearch config not found", null);         
        var openAIConfig = cfg.GetSection("KernelMemory:Service:OpenAI").Get<OpenAIConfig>();
        var useRealEmbeddingGenerator = cfg.GetValue<bool>("UseRealEmbeddingGenerator");
        ITextEmbeddingGenerator embeddingGenerator;

        if (useRealEmbeddingGenerator)
        {
            embeddingGenerator = new OpenAITextEmbeddingGenerator(openAIConfig, log: null);
        } 
        else
        {
            embeddingGenerator = new MockEmbeddingGenerator();
        }

        var memory = new AzureCosmosDBMongoDBMemory(config, embeddingGenerator, DatabaseName, CollectionName);

        Embedding embedding1 = new[] { 0f, 0, 1, 0, 1 };
        Embedding embedding2 = new[] { 0, 0, 0.95f, 0.01f, 0.95f };
        if (useRealEmbeddingGenerator)
        {
            embedding1 = await embeddingGenerator.GenerateEmbeddingAsync(Text1);
            embedding2 = await embeddingGenerator.GenerateEmbeddingAsync(Text2);
        }
        else
        {
            ((MockEmbeddingGenerator)embeddingGenerator).AddFakeEmbedding(Text1, embedding1);
            ((MockEmbeddingGenerator)embeddingGenerator).AddFakeEmbedding(Text2, embedding2);
        }

        return (memory, new [] {embedding1, embedding2 });

    }


}
