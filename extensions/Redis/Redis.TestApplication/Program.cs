// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.MemoryDb.Redis;
using Microsoft.KernelMemory.MemoryStorage;
using StackExchange.Redis;

namespace Microsoft.Redis.TestApplication;

public static class Program
{
    private const string Text1 = "this is test 1";
    private const string Text2 = "this is test 2";

    public static async Task Main()
    {
        var (memory, embeddings) = await SetupAsync();

        Console.WriteLine("===== DELETE INDEX =====");

        await memory.DeleteIndexAsync("test");
        await memory.DeleteIndexAsync("test1");
        await memory.DeleteIndexAsync("test2");

        Console.WriteLine("===== CREATE INDEXES =====");

        await memory.CreateIndexAsync("test", embeddings[0].Length);
        await memory.CreateIndexAsync("test1", embeddings[0].Length);

        Console.WriteLine("===== LIST INDEXES =====");

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

        var id1 = await memory.UpsertAsync("test", memoryRecord1);
        Console.WriteLine($"Insert 1: {id1} {memoryRecord1.Id}");

        var id2 = await memory.UpsertAsync("test2", memoryRecord2);
        Console.WriteLine($"Insert 2: {id2} {memoryRecord2.Id}");

        Console.WriteLine("===== LIST INDEXES =====");

        indexes = await memory.GetIndexesAsync();
        foreach (var indexName in indexes)
        {
            Console.WriteLine(indexName);
        }

        Console.WriteLine("===== INSERT RECORD 3 =====");

        var memoryRecord3 = new MemoryRecord
        {
            Id = "memory three",
            Vector = embeddings[1],
            Tags = new TagCollection { { "type", "news" } },
            Payload = new Dictionary<string, object>()
        };

        var id3 = await memory.UpsertAsync("test", memoryRecord3);
        Console.WriteLine($"Insert 3: {id3} {memoryRecord3.Id}");

        Console.WriteLine("===== UPDATE RECORD 3 =====");

        memoryRecord3.Tags.Add("updated", "yes");
        id3 = await memory.UpsertAsync("test", memoryRecord3);
        Console.WriteLine($"Update 3: {id3} {memoryRecord3.Id}");

        Console.WriteLine("===== SEARCH 1 =====");

        var similarList = memory.GetSimilarListAsync(
            "test", text: Text1, limit: 10, withEmbeddings: true);
        await foreach ((MemoryRecord, double) record in similarList)
        {
            Console.WriteLine(record.Item1.Id);
            Console.WriteLine("  tags: " + record.Item1.Tags.Count);
            Console.WriteLine("  size: " + record.Item1.Vector.Length);
        }

        Console.WriteLine("===== SEARCH 2 =====");

        similarList = memory.GetSimilarListAsync(
            "test", text: Text1, limit: 10, withEmbeddings: true,
            filters: new List<MemoryFilter> { MemoryFilters.ByTag("type", "email") });
        await foreach ((MemoryRecord, double) record in similarList)
        {
            Console.WriteLine(record.Item1.Id);
            Console.WriteLine("  type: " + record.Item1.Tags["type"].First());
        }

        Console.WriteLine("===== LIST =====");

        var list = memory.GetListAsync("test", limit: 10, withEmbeddings: false);
        await foreach (MemoryRecord record in list)
        {
            Console.WriteLine(record.Id);
            Console.WriteLine("  type: " + record.Tags["type"].First());
        }

        Console.WriteLine("===== DELETE =====");

        await memory.DeleteAsync("test", new MemoryRecord { Id = "memory 1" });

        Console.WriteLine("===== LIST AFTER DELETE =====");

        list = memory.GetListAsync("test", limit: 10, withEmbeddings: false);
        await foreach (MemoryRecord record in list)
        {
            Console.WriteLine(record.Id);
            Console.WriteLine("  type: " + record.Tags["type"].First());
        }

        Console.WriteLine("== Done ==");
    }

    private static async Task<(RedisMemory, Embedding[])> SetupAsync()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.development.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var useRealEmbeddingGenerator = config.GetValue<bool>("UseRealEmbeddingGenerator");
        var connectionString = config.GetValue<string>("RedisConnectionString") ?? string.Empty;
        var openAIApiKey = config.GetValue<string>("OpenAiApiKey") ?? string.Empty;

        // ======================================================
        // ======================================================
        // ======================================================

        ITextEmbeddingGenerator embeddingGenerator;
        if (useRealEmbeddingGenerator)
        {
            embeddingGenerator = new OpenAITextEmbeddingGenerator(new OpenAIConfig
            {
                EmbeddingModel = "text-embedding-ada-002",
                EmbeddingModelMaxTokenTotal = 8_191,
                APIKey = openAIApiKey
            }, loggerFactory: null);
        }
        else
        {
            embeddingGenerator = new MockEmbeddingGenerator();
        }

        var tags = new Dictionary<string, char?>
        {
            { "updated", '|' },
            { "type", '|' }
        };

        var muxer = await ConnectionMultiplexer.ConnectAsync(connectionString);
        var memory = new RedisMemory(new RedisConfig(tags: tags), muxer, embeddingGenerator);

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

        // ======================================================
        // ======================================================
        // ======================================================

        return (memory, new[] { embedding1, embedding2 });
    }
}
