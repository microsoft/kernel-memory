// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.MemoryDb.Qdrant;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KM.TestHelpers;

namespace Microsoft.Qdrant.TestApplication;

public static class Program
{
    public static async Task Main()
    {
        var cfg = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.development.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var config = cfg.GetSection("KernelMemory:Services:Qdrant").Get<QdrantConfig>();
        ArgumentNullExceptionEx.ThrowIfNull(config, nameof(config), "Qdrant config not found");

        var embeddingGenerator = new FakeEmbeddingGenerator();

        var memory = new QdrantMemory(config, embeddingGenerator);

        Console.WriteLine("===== DELETE INDEX =====");

        await memory.DeleteIndexAsync("test");

        Console.WriteLine("===== CREATE INDEX =====");

        await memory.CreateIndexAsync("test", 5);

        Console.WriteLine("===== INSERT RECORD 1 =====");

        const string Text = "test1";
        var embedding = new[] { 0f, 0, 1, 0, 1 };
        embeddingGenerator.Mock(Text, embedding);

        var memoryRecord1 = new MemoryRecord
        {
            Id = "memory 1",
            Vector = embedding,
            Tags = new TagCollection { { "updated", "no" }, { "type", "email" } },
            Payload = []
        };

        var id1 = await memory.UpsertAsync("test", memoryRecord1);
        Console.WriteLine($"Insert 1: {id1} {memoryRecord1.Id}");

        Console.WriteLine("===== INSERT RECORD 2 =====");

        var memoryRecord2 = new MemoryRecord
        {
            Id = "memory two",
            Vector = new[] { 0f, 0, 1, 0, 1 },
            Tags = new TagCollection { { "type", "news" } },
            Payload = []
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
            limit: 10, withEmbeddings: true, filters: [MemoryFilters.ByTag("type", "email")]);
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
}
