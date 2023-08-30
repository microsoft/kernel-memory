// Copyright (c) Microsoft. All rights reserved.

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
            ["01"] = new() { Id = "01", Vector = new ReadOnlyMemory<float>(new[] { 0.1f, 0.1f, 0.1f }) },
            ["02"] = new() { Id = "02", Vector = new ReadOnlyMemory<float>(new[] { 0.81f, 0.12f, 0.13f }) },
            ["03"] = new() { Id = "03", Vector = new ReadOnlyMemory<float>(new[] { 0.25f, 0.25f, 0.35f }) },
            ["04"] = new() { Id = "04", Vector = new ReadOnlyMemory<float>(new[] { 0.05f, 0.91f, 0.03f }) },
        };

        foreach (KeyValuePair<string, MemoryRecord> r in records)
        {
            await acs.UpsertAsync(indexName, r.Value);
            await qdrant.UpsertAsync(indexName, r.Value);
            await simpleVecDb.UpsertAsync(indexName, r.Value);
        }

        await Task.Delay(TimeSpan.FromSeconds(2));

        var target = new ReadOnlyMemory<float>(new[] { 0.01f, 0.5f, 0.01f });
        IAsyncEnumerable<(MemoryRecord, double)> acsList = acs.GetSimilarListAsync(indexName, target, 10, withEmbeddings: true);
        IAsyncEnumerable<(MemoryRecord, double)> qdrantList = qdrant.GetSimilarListAsync(indexName, target, 10, withEmbeddings: true);
        IAsyncEnumerable<(MemoryRecord, double)> simpleVecDbList = simpleVecDb.GetSimilarListAsync(indexName, target, 10, withEmbeddings: true);

        var acsResults = await acsList.ToListAsync();
        var qdrantResults = await qdrantList.ToListAsync();
        var simpleVecDbResults = await simpleVecDbList.ToListAsync();

        this._log.WriteLine($"Azure Cognitive Search: {acsResults.Count} results");
        foreach ((MemoryRecord, double) r in acsResults)
        {
            this._log.WriteLine($" - ID: {r.Item1.Id}, Distance: {r.Item2}, Expected distance: {CosineSim(target.ToArray(), records[r.Item1.Id].Vector.ToArray())}");
        }

        this._log.WriteLine($"\n\nQdrant: {qdrantResults.Count} results");
        foreach ((MemoryRecord, double) r in qdrantResults)
        {
            this._log.WriteLine($" - ID: {r.Item1.Id}, Distance: {r.Item2}, Expected distance: {CosineSim(target.ToArray(), records[r.Item1.Id].Vector.ToArray())}");
        }

        this._log.WriteLine($"\n\nSimple vector DB: {simpleVecDbResults.Count} results");
        foreach ((MemoryRecord, double) r in simpleVecDbResults)
        {
            this._log.WriteLine($" - ID: {r.Item1.Id}, Distance: {r.Item2}, Expected distance: {CosineSim(target.ToArray(), records[r.Item1.Id].Vector.ToArray())}");
        }
    }

    private static double CosineSim(IEnumerable<float> vec1, IEnumerable<float> vec2)
    {
        var v1 = vec1.ToArray();
        var v2 = vec2.ToArray();

        if (v1.Length != v2.Length)
        {
            throw new Exception($"Vector size should be the same: {v1.Length} != {v2.Length}");
        }

        int size = v1.Length;
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
