﻿// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory;
using Microsoft.SemanticMemory.MemoryStorage;
using Microsoft.SemanticMemory.MemoryStorage.AzureCognitiveSearch;
using Microsoft.SemanticMemory.MemoryStorage.DevTools;
using Microsoft.SemanticMemory.MemoryStorage.Qdrant;
using Xunit.Abstractions;

// ReSharper disable InconsistentNaming

namespace FunctionalTests.VectorDbComparison;

public class TestCosineSimilarity
{
    private readonly IConfiguration _cfg;
    private readonly ITestOutputHelper _log;

    public TestCosineSimilarity(IConfiguration cfg, ITestOutputHelper log)
    {
        this._cfg = cfg;
        this._log = log;
    }

    [Fact]
    public async Task CompareCosineSimilarity()
    {
        const string indexName = "tests";

        var acs = new AzureCognitiveSearchMemory(
            this._cfg.GetSection("Services").GetSection("AzureCognitiveSearch")
                .Get<AzureCognitiveSearchConfig>()!);

        var qdrant = new QdrantMemory(
            this._cfg.GetSection("Services").GetSection("Qdrant")
                .Get<QdrantConfig>()!);

        var simpleVecDb = new SimpleVectorDb(
            this._cfg.GetSection("Services").GetSection("SimpleVectorDb")
                .Get<SimpleVectorDbConfig>()!);

        await acs.DeleteIndexAsync(indexName);
        await qdrant.DeleteIndexAsync(indexName);
        await simpleVecDb.DeleteIndexAsync(indexName);

        await Task.Delay(TimeSpan.FromSeconds(2));

        await acs.CreateIndexAsync(indexName, 3);
        await qdrant.CreateIndexAsync(indexName, 3);
        await simpleVecDb.CreateIndexAsync(indexName, 3);

        var records = new Dictionary<string, MemoryRecord>
        {
            ["1"] = new() { Id = "1", Vector = new[] { 0.25f, 0.33f, 0.29f } },
            ["2"] = new() { Id = "2", Vector = new[] { 0.25f, 0.25f, 0.35f } },
            ["3"] = new() { Id = "3", Vector = new[] { 0.1f, 0.1f, 0.1f } },
            ["4"] = new() { Id = "4", Vector = new[] { 0.05f, 0.91f, 0.03f } },
            ["5"] = new() { Id = "5", Vector = new[] { 0.65f, 0.12f, 0.99f } },
            ["6"] = new() { Id = "6", Vector = new[] { 0.81f, 0.12f, 0.13f } },
            ["7"] = new() { Id = "7", Vector = new[] { 0.88f, 0.01f, 0.13f } },
        };

        foreach (KeyValuePair<string, MemoryRecord> r in records)
        {
            await acs.UpsertAsync(indexName, r.Value);
            await qdrant.UpsertAsync(indexName, r.Value);
            await simpleVecDb.UpsertAsync(indexName, r.Value);
        }

        await Task.Delay(TimeSpan.FromSeconds(2));
        var target = new[] { 0.01f, 0.5f, 0.41f };
        IAsyncEnumerable<(MemoryRecord, double)> acsList = acs.GetSimilarListAsync(indexName, target, 10, withEmbeddings: true);
        IAsyncEnumerable<(MemoryRecord, double)> qdrantList = qdrant.GetSimilarListAsync(indexName, target, 10, withEmbeddings: true);
        IAsyncEnumerable<(MemoryRecord, double)> simpleVecDbList = simpleVecDb.GetSimilarListAsync(indexName, target, 10, withEmbeddings: true);

        var acsResults = await acsList.ToListAsync();
        var qdrantResults = await qdrantList.ToListAsync();
        var simpleVecDbResults = await simpleVecDbList.ToListAsync();

        this._log.WriteLine($"Azure Cognitive Search: {acsResults.Count} results");
        foreach ((MemoryRecord, double) r in acsResults)
        {
            var actual = r.Item2;
            var expected = CosineSim(target, records[r.Item1.Id].Vector);
            var diff = expected - actual;
            this._log.WriteLine($" - ID: {r.Item1.Id}, Distance: {actual}, Expected distance: {expected}, Difference: {diff:0.0000000000}");
        }

        this._log.WriteLine($"\n\nQdrant: {qdrantResults.Count} results");
        foreach ((MemoryRecord, double) r in qdrantResults)
        {
            var actual = r.Item2;
            var expected = CosineSim(target, records[r.Item1.Id].Vector);
            var diff = expected - actual;
            this._log.WriteLine($" - ID: {r.Item1.Id}, Distance: {actual}, Expected distance: {expected}, Difference: {diff:0.0000000000}");
        }

        this._log.WriteLine($"\n\nSimple vector DB: {simpleVecDbResults.Count} results");
        foreach ((MemoryRecord, double) r in simpleVecDbResults)
        {
            var actual = r.Item2;
            var expected = CosineSim(target, records[r.Item1.Id].Vector);
            var diff = expected - actual;
            this._log.WriteLine($" - ID: {r.Item1.Id}, Distance: {actual}, Expected distance: {expected}, Difference: {diff:0.0000000000}");
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
