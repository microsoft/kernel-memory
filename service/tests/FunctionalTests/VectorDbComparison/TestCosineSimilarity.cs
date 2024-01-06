// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.MemoryDb.AzureAISearch;
using Microsoft.KernelMemory.MemoryDb.Qdrant;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Microsoft.KernelMemory.Postgres;
using Microsoft.TestHelpers;
using Xunit.Abstractions;

// ReSharper disable MissingBlankLines

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
        const bool AzSearchEnabled = true;
        const bool QdrantEnabled = true;
        const bool PostgresEnabled = true;

        // == Ctors

        var embeddingGenerator = new FakeEmbeddingGenerator();
        var acs = new AzureAISearchMemory(this.AzureAiSearchConfig, embeddingGenerator);
        var qdrant = new QdrantMemory(this.QdrantConfig, embeddingGenerator);
        var postgres = new PostgresMemory(this.PostgresConfig, embeddingGenerator);
        var simpleVecDb = new SimpleVectorDb(this.SimpleVectorDbConfig, embeddingGenerator);

        // == Delete indexes left over

        if (AzSearchEnabled) { await acs.DeleteIndexAsync(IndexName); }
        if (PostgresEnabled) { await postgres.DeleteIndexAsync(IndexName); }
        if (QdrantEnabled) { await qdrant.DeleteIndexAsync(IndexName); }
        await simpleVecDb.DeleteIndexAsync(IndexName);

        await Task.Delay(TimeSpan.FromSeconds(2));

        // == Create indexes

        if (AzSearchEnabled) { await acs.CreateIndexAsync(IndexName, 3); }
        if (PostgresEnabled) { await postgres.CreateIndexAsync(IndexName, 3); }
        if (QdrantEnabled) { await qdrant.CreateIndexAsync(IndexName, 3); }
        await simpleVecDb.CreateIndexAsync(IndexName, 3);

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
            if (AzSearchEnabled) { await acs.UpsertAsync(IndexName, r.Value); }
            if (PostgresEnabled) { await postgres.UpsertAsync(IndexName, r.Value); }
            if (QdrantEnabled) { await qdrant.UpsertAsync(IndexName, r.Value); }
            await simpleVecDb.UpsertAsync(IndexName, r.Value);
        }

        await Task.Delay(TimeSpan.FromSeconds(2));

        // == Search by similarity

        var target = new[] { 0.01f, 0.5f, 0.41f };
        embeddingGenerator.Mock("text01", target);

        IAsyncEnumerable<(MemoryRecord, double)> acsList;
        IAsyncEnumerable<(MemoryRecord, double)> postgresList;
        IAsyncEnumerable<(MemoryRecord, double)> qdrantList;
        if (AzSearchEnabled)
        {
            acsList = acs.GetSimilarListAsync(
                index: IndexName, text: "text01", limit: 10, withEmbeddings: true);
        }

        if (PostgresEnabled)
        {
            postgresList = postgres.GetSimilarListAsync(
                index: IndexName, text: "text01", limit: 10, withEmbeddings: true);
        }

        if (QdrantEnabled)
        {
            qdrantList = qdrant.GetSimilarListAsync(
                index: IndexName, text: "text01", limit: 10, withEmbeddings: true);
        }

        IAsyncEnumerable<(MemoryRecord, double)> simpleVecDbList = simpleVecDb.GetSimilarListAsync(
            index: IndexName, text: "text01", limit: 10, withEmbeddings: true);

        List<(MemoryRecord, double)> acsResults;
        List<(MemoryRecord, double)> postgresResults;
        List<(MemoryRecord, double)> qdrantResults;
        if (AzSearchEnabled)
        {
            acsResults = await acsList.ToListAsync();
        }

        if (PostgresEnabled)
        {
            postgresResults = await postgresList.ToListAsync();
        }

        if (QdrantEnabled)
        {
            qdrantResults = await qdrantList.ToListAsync();
        }

        var simpleVecDbResults = await simpleVecDbList.ToListAsync();

        // == Test results: test precision and ordering

        const double Precision = 0.000001d;
        var previous = "0";

        if (AzSearchEnabled)
        {
            this._log.WriteLine($"Azure AI Search: {acsResults.Count} results");
            previous = "0";
            foreach ((MemoryRecord? memoryRecord, double actual) in acsResults)
            {
                var expected = CosineSim(target, records[memoryRecord.Id].Vector);
                var diff = expected - actual;
                this._log.WriteLine($" - ID: {memoryRecord.Id}, Distance: {actual}, Expected distance: {expected}, Difference: {diff:0.0000000000}");
                Assert.True(Math.Abs(diff) < Precision);
                Assert.True(string.Compare(memoryRecord.Id, previous, StringComparison.OrdinalIgnoreCase) > 0, "Records are not ordered by similarity");
                previous = memoryRecord.Id;
            }
        }

        if (PostgresEnabled)
        {
            this._log.WriteLine($"\n\nPostgres: {postgresResults.Count} results");
            previous = "0";
            foreach ((MemoryRecord memoryRecord, double actual) in postgresResults)
            {
                var expected = CosineSim(target, records[memoryRecord.Id].Vector);
                var diff = expected - actual;
                this._log.WriteLine($" - ID: {memoryRecord.Id}, Distance: {actual}, Expected distance: {expected}, Difference: {diff:0.0000000000}");
                Assert.True(Math.Abs(diff) < Precision);
                Assert.True(string.Compare(memoryRecord.Id, previous, StringComparison.OrdinalIgnoreCase) > 0, "Records are not ordered by similarity");
                previous = memoryRecord.Id;
            }
        }

        if (QdrantEnabled)
        {
            this._log.WriteLine($"\n\nQdrant: {qdrantResults.Count} results");
            previous = "0";
            foreach ((MemoryRecord memoryRecord, double actual) in qdrantResults)
            {
                var expected = CosineSim(target, records[memoryRecord.Id].Vector);
                var diff = expected - actual;
                this._log.WriteLine($" - ID: {memoryRecord.Id}, Distance: {actual}, Expected distance: {expected}, Difference: {diff:0.0000000000}");
                Assert.True(Math.Abs(diff) < Precision);
                Assert.True(string.Compare(memoryRecord.Id, previous, StringComparison.OrdinalIgnoreCase) > 0, "Records are not ordered by similarity");
                previous = memoryRecord.Id;
            }
        }

        this._log.WriteLine($"\n\nSimple vector DB: {simpleVecDbResults.Count} results");
        previous = "0";
        foreach ((MemoryRecord memoryRecord, double actual) in simpleVecDbResults)
        {
            var expected = CosineSim(target, records[memoryRecord.Id].Vector);
            var diff = expected - actual;
            this._log.WriteLine($" - ID: {memoryRecord.Id}, Distance: {actual}, Expected distance: {expected}, Difference: {diff:0.0000000000}");
            Assert.True(Math.Abs(diff) < Precision);
            Assert.True(string.Compare(memoryRecord.Id, previous, StringComparison.OrdinalIgnoreCase) > 0, "Records are not ordered by similarity");
            previous = memoryRecord.Id;
        }
    }

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
