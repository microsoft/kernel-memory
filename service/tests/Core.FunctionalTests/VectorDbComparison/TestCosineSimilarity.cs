// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.MemoryDb.AzureAISearch;
using Microsoft.KernelMemory.MemoryDb.Qdrant;
using Microsoft.KernelMemory.MemoryDb.Redis;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Microsoft.KernelMemory.MongoDbAtlas;
using Microsoft.KernelMemory.Postgres;
using Microsoft.TestHelpers;
using StackExchange.Redis;
using Xunit.Abstractions;

namespace FunctionalTests.VectorDbComparison;

public class TestCosineSimilarity : BaseFunctionalTestCase
{
    private const string IndexName = "test-cosinesimil";

    private readonly ITestOutputHelper _log;

    public TestCosineSimilarity(IConfiguration cfg, ITestOutputHelper log) : base(cfg, log)
    {
        this._log = log;
    }

    [Fact]
    [Trait("Category", "Serverless")]
    public async Task CompareCosineSimilarity()
    {
        const bool SimpleDbEnabled = true;
        const bool AzSearchEnabled = true;
        const bool QdrantEnabled = true;
        const bool PostgresEnabled = true;
        const bool RedisEnabled = true;
        const bool MongoDbAtlasEnabled = true;

        // == Ctors
        var embeddingGenerator = new FakeEmbeddingGenerator();

        SimpleVectorDb? simpleVecDb = null;
        if (SimpleDbEnabled) { simpleVecDb = new SimpleVectorDb(this.SimpleVectorDbConfig, embeddingGenerator); }

        AzureAISearchMemory? acs = null;
        if (AzSearchEnabled) { acs = new AzureAISearchMemory(this.AzureAiSearchConfig, embeddingGenerator); }

        QdrantMemory? qdrant = null;
        if (QdrantEnabled) { qdrant = new QdrantMemory(this.QdrantConfig, embeddingGenerator); }

        PostgresMemory? postgres = null;
        if (PostgresEnabled) { postgres = new PostgresMemory(this.PostgresConfig, embeddingGenerator); }

        MongoDbAtlasMemory? atlasVectorDb = null;
        if (MongoDbAtlasEnabled) { atlasVectorDb = new MongoDbAtlasMemory(this.MongoDbAtlasConfig, embeddingGenerator); }

        RedisMemory? redis = null;
        if (RedisEnabled)
        {
            // TODO: revisit RedisMemory not to need this, e.g. not to connect in ctor
            var redisMux = await ConnectionMultiplexer.ConnectAsync(this.RedisConfig.ConnectionString);
            redis = new RedisMemory(this.RedisConfig, redisMux, embeddingGenerator);
        }

        var dbs = new IMemoryDb[] { simpleVecDb!, acs!, postgres!, qdrant!, redis!, atlasVectorDb! };

        // == Delete indexes left over

        await this.DeleteIndexAsync(IndexName, dbs);
        await Task.Delay(TimeSpan.FromSeconds(2));

        // == Create indexes

        await this.CreateIndexAsync(IndexName, 3, dbs);
        await Task.Delay(TimeSpan.FromSeconds(1));

        // == Insert data. Note: records are inserted out of order on purpose.

        var records = new Dictionary<string, MemoryRecord>
        {
            ["3"] = new() { Id = "3", Vector = new[] { 0.1f, 0.1f, 0.1f } },
            ["2"] = new() { Id = "2", Vector = new[] { 0.25f, 0.25f, 0.35f } },
            ["1"] = new() { Id = "1", Vector = new[] { 0.25f, 0.33f, 0.29f } },
            ["5"] = new() { Id = "5", Vector = new[] { 0.65f, 0.12f, 0.99f } },
            ["4"] = new() { Id = "4", Vector = new[] { 0.05f, 0.91f, 0.03f } },
            ["7"] = new() { Id = "7", Vector = new[] { 0.88f, 0.01f, 0.13f } },
            ["6"] = new() { Id = "6", Vector = new[] { 0.81f, 0.12f, 0.13f } },
        };

        foreach (KeyValuePair<string, MemoryRecord> r in records)
        {
            await this.UpsertAsync(IndexName, r.Value, dbs);
        }

        await Task.Delay(TimeSpan.FromSeconds(2));

        // == Test results: test precision and ordering

        var target = new[] { 0.01f, 0.5f, 0.41f };
        embeddingGenerator.Mock("text01", target);

        await this.TestSimilarityAsync(records, dbs);
    }

    private async Task DeleteIndexAsync(string indexName, IMemoryDb[] memoryDbs)
    {
        foreach (var memoryDb in memoryDbs.Where(x => x != null))
        {
            this._log.WriteLine($"Deleting index {indexName} in {memoryDb.GetType().FullName}");
            await memoryDb.DeleteIndexAsync(indexName);
        }
    }

    private async Task CreateIndexAsync(string indexName, int vectorSize, IMemoryDb[] memoryDbs)
    {
        foreach (var memoryDb in memoryDbs.Where(x => x != null))
        {
            this._log.WriteLine($"Creating index {indexName} in {memoryDb.GetType().FullName}");
            await memoryDb.CreateIndexAsync(indexName, vectorSize);
        }
    }

    private async Task UpsertAsync(string indexName, MemoryRecord record, IMemoryDb[] memoryDbs)
    {
        foreach (var memoryDb in memoryDbs.Where(x => x != null))
        {
            this._log.WriteLine($"Adding record in {memoryDb.GetType().FullName}");
            await memoryDb.UpsertAsync(indexName, record);
        }
    }

    private async Task TestSimilarityAsync(Dictionary<string, MemoryRecord> records, IMemoryDb[] memoryDbs)
    {
        var target = new[] { 0.01f, 0.5f, 0.41f };

        foreach (var memoryDb in memoryDbs.Where(x => x != null))
        {
            const double Precision = 0.000001d;
            var previous = "0";

            IAsyncEnumerable<(MemoryRecord, double)> list = memoryDb.GetSimilarListAsync(
                index: IndexName, text: "text01", limit: 10, withEmbeddings: true);
            List<(MemoryRecord, double)> results = await list.ToListAsync();

            this._log.WriteLine($"\n\n{memoryDb.GetType().FullName}: {results.Count} results");
            previous = "0";
            foreach ((MemoryRecord? memoryRecord, double actual) in results)
            {
                var expected = CosineSim(target, records[memoryRecord.Id].Vector);
                var diff = expected - actual;
                this._log.WriteLine($" - ID: {memoryRecord.Id}, Distance: {actual}, Expected distance: {expected}, Difference: {diff:0.0000000000}");
                Assert.True(Math.Abs(diff) < Precision);
                Assert.True(string.Compare(memoryRecord.Id, previous, StringComparison.OrdinalIgnoreCase) > 0, "Records are not ordered by similarity");
                previous = memoryRecord.Id;
            }
        }
    }

    // Note: not using external libraries to have complete control on the expected value.
    private static double CosineSim(Embedding vec1, Embedding vec2)
    {
        var v1 = vec1.Data.ToArray();
        var v2 = vec2.Data.ToArray();

        if (vec1.Length != vec2.Length)
        {
            throw new Exception($"Vector size should be the same: {vec1.Length} != {vec2.Length}");
        }

        int size = vec1.Length;
        double dot = 0.0d;
        double m1 = 0.0d;
        double m2 = 0.0d;
        for (int n = 0; n < size; n++)
        {
            dot += v1[n] * v2[n];
            m1 += Math.Pow(v1[n], 2);
            m2 += Math.Pow(v2[n], 2);
        }

        double cosineSimilarity = dot / (Math.Sqrt(m1) * Math.Sqrt(m2));
        return cosineSimilarity;
    }
}
