// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.MemoryDb.Redis;
using Microsoft.KernelMemory.MemoryStorage;
using StackExchange.Redis;

public static class Program
{
    private const string Text = "test1";

    public static async Task Main()
    {
        var (memory, embedding) = await SetupAsync();

        Console.WriteLine("===== DELETE INDEX =====");

        await memory.DeleteIndexAsync("test");

        Console.WriteLine("===== CREATE INDEX =====");

        await memory.CreateIndexAsync("test", 5);

        Console.WriteLine("===== INSERT RECORD 1 =====");

        var memoryRecord1 = new MemoryRecord
        {
            Id = "memory 1",
            Vector = embedding,
            Tags = new TagCollection { { "updated", "no" }, { "type", "email" } },
            Payload = new Dictionary<string, object>()
        };

        var id1 = await memory.UpsertAsync("test", memoryRecord1);
        Console.WriteLine($"Insert 1: {id1} {memoryRecord1.Id}");

        Console.WriteLine("===== INSERT RECORD 2 =====");

        var memoryRecord2 = new MemoryRecord
        {
            Id = "memory two",
            Vector = new[] { 0f, 0, 1, 0, 1 },
            Tags = new TagCollection { { "type", "news" } },
            Payload = new Dictionary<string, object>()
        };

        var id2 = await memory.UpsertAsync("test", memoryRecord2);
        Console.WriteLine($"Insert 2: {id2} {memoryRecord2.Id}");

        Console.WriteLine("===== UPDATE RECORD 2 =====");

        memoryRecord2.Tags.Add("updated", "yes");
        id2 = await memory.UpsertAsync("test", memoryRecord2);
        Console.WriteLine($"Update 2: {id2} {memoryRecord2.Id}");

        Console.WriteLine("===== SEARCH 1 =====");

        var similarList = memory.GetSimilarListAsync("test", text: Text,
            limit: 10, withEmbeddings: true);
        await foreach ((MemoryRecord, double) record in similarList)
        {
            Console.WriteLine(record.Item1.Id);
            Console.WriteLine("  tags: " + record.Item1.Tags.Count);
            Console.WriteLine("  size: " + record.Item1.Vector.Length);
        }

        Console.WriteLine("===== SEARCH 2 =====");

        similarList = memory.GetSimilarListAsync("test", text: Text,
            limit: 10, withEmbeddings: true, filters: new List<MemoryFilter> { MemoryFilters.ByTag("type", "email") });
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

    private static async Task<(RedisMemory, Embedding)> SetupAsync()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
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
            }, log: null);
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
        RedisMemory memory = new RedisMemory(new RedisConfig(tags: tags), muxer, embeddingGenerator);

        Embedding embedding;
        if (useRealEmbeddingGenerator)
        {
            embedding = await embeddingGenerator.GenerateEmbeddingAsync(Text);
        }
        else
        {
            embedding = new[] { 0f, 0, 1, 0, 1 };
            ((MockEmbeddingGenerator)embeddingGenerator).AddFakeEmbedding(Text, embedding);
        }

        // ======================================================
        // ======================================================
        // ======================================================

        return (memory, embedding);
    }
}
