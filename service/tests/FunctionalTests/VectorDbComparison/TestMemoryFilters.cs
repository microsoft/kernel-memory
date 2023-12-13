// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.MemoryStorage.AzureAISearch;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Microsoft.KernelMemory.MemoryStorage.Qdrant;
using Xunit.Abstractions;

namespace FunctionalTests.VectorDbComparison;

public class TestMemoryFilters
{
    private const string IndexName = "filterstests";

    private readonly IConfiguration _cfg;
    private readonly ITestOutputHelper _log;

    public TestMemoryFilters(IConfiguration cfg, ITestOutputHelper log)
    {
        this._cfg = cfg;
        this._log = log;
    }

    [Fact]
    public async Task TestFilters()
    {
        // Booleans used for investigating test failures
        const bool DeleteIndex = true;
        const bool CreateIndex = true;
        const bool CreateRecords = true;

        var embeddingGenerator = new FakeEmbeddingGenerator();

        var acs = new AzureAISearchMemory(
            this._cfg.GetSection("Services").GetSection("AzureAISearch")
                .Get<AzureAISearchConfig>()!, embeddingGenerator);

        var qdrant = new QdrantMemory(
            this._cfg.GetSection("Services").GetSection("Qdrant")
                .Get<QdrantConfig>()!, embeddingGenerator);

        var simpleVecDb = new SimpleVectorDb(
            this._cfg.GetSection("Services").GetSection("SimpleVectorDb")
                .Get<SimpleVectorDbConfig>()!, embeddingGenerator);

        if (DeleteIndex)
        {
            await acs.DeleteIndexAsync(IndexName);
            await qdrant.DeleteIndexAsync(IndexName);
            await simpleVecDb.DeleteIndexAsync(IndexName);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        if (CreateIndex)
        {
            await acs.CreateIndexAsync(IndexName, 3);
            await qdrant.CreateIndexAsync(IndexName, 3);
            await simpleVecDb.CreateIndexAsync(IndexName, 3);
        }

        if (CreateRecords)
        {
            var records = new Dictionary<string, MemoryRecord>
            {
                ["1"] = new() { Id = "1", Vector = new[] { 0.25f, 0.33f, 0.29f }, Tags = new() { { "user", "Kaylee" }, { "collection", "Work" } } },
                ["2"] = new() { Id = "2", Vector = new[] { 0.25f, 0.25f, 0.35f }, Tags = new() { { "user", "Kaylee" }, { "collection", "Personal" } } },
                ["3"] = new() { Id = "3", Vector = new[] { 0.1f, 0.1f, 0.1f }, Tags = new() { { "user", "Kaylee" }, { "collection", "Family" } } },
                ["4"] = new() { Id = "4", Vector = new[] { 0.05f, 0.91f, 0.03f }, Tags = new() { { "user", "Kaylee" }, { "collection", "Family" } } },
                ["5"] = new() { Id = "5", Vector = new[] { 0.65f, 0.12f, 0.99f }, Tags = new() { { "user", "Kaylee" }, { "collection", "Family" } } },
                ["6"] = new() { Id = "6", Vector = new[] { 0.81f, 0.12f, 0.13f }, Tags = new() { { "user", "Madelynn" }, { "collection", "Personal" } } },
                ["7"] = new() { Id = "7", Vector = new[] { 0.88f, 0.01f, 0.13f }, Tags = new() { { "user", "Madelynn" }, { "collection", "Work" } } },
            };

            foreach (KeyValuePair<string, MemoryRecord> r in records)
            {
                await acs.UpsertAsync(IndexName, r.Value);
                await qdrant.UpsertAsync(IndexName, r.Value);
                await simpleVecDb.UpsertAsync(IndexName, r.Value);
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        this._log.WriteLine($"\n\n\n----- Azure AI Search -----");
        await this.TestVectorDbFiltering(acs);
        this._log.WriteLine($"\n\n\n----- Qdrant vector DB -----");
        await this.TestVectorDbFiltering(qdrant);
        this._log.WriteLine($"\n\n\n----- Simple vector DB -----");
        await this.TestVectorDbFiltering(simpleVecDb);
    }

    private async Task TestVectorDbFiltering(IMemoryDb vectorDb)
    {
        // Single memory filter
        var singleFilter = new List<MemoryFilter>() { MemoryFilters.ByTag("user", "Kaylee") };
        var singleFilterResults = await vectorDb.GetListAsync(IndexName, filters: singleFilter, limit: int.MaxValue).ToListAsync();
        this._log.WriteLine($"\n\nSingle memory filter: {singleFilterResults.Count} results");
        foreach (MemoryRecord r in singleFilterResults)
        {
            this._log.WriteLine($" - ID: {r.Id}, Tags: {string.Join(", ", r.Tags.Select(t => $"{t.Key}: {string.Join(", ", t.Value)}"))}");
        }

        // Single memory filter with multiple tags
        var singleFilterMultipleTags = new List<MemoryFilter>() { MemoryFilters.ByTag("user", "Kaylee").ByTag("collection", "Work") };
        var singleFilterMultipleTagsResults = await vectorDb.GetListAsync(IndexName, filters: singleFilterMultipleTags, limit: int.MaxValue).ToListAsync();
        this._log.WriteLine($"\n\nSingle memory filter with multiple tags: {singleFilterMultipleTagsResults.Count} results");
        foreach (MemoryRecord r in singleFilterMultipleTagsResults)
        {
            this._log.WriteLine($" - ID: {r.Id}, Tags: {string.Join(", ", r.Tags.Select(t => $"{t.Key}: {string.Join(", ", t.Value)}"))}");
        }

        // Multiple memory filters with multiple tags
        var multipleFilters = new List<MemoryFilter>()
        {
            MemoryFilters.ByTag("user", "Kaylee").ByTag("collection", "Family"),
            MemoryFilters.ByTag("user", "Madelynn").ByTag("collection", "Personal")
        };
        var multipleFiltersResults = await vectorDb.GetListAsync(IndexName, filters: multipleFilters, limit: int.MaxValue).ToListAsync();
        this._log.WriteLine($"\n\nMultiple memory filters with multiple tags: {multipleFiltersResults.Count} results");
        foreach (MemoryRecord r in multipleFiltersResults)
        {
            this._log.WriteLine($" - ID: {r.Id}, Tags: {string.Join(", ", r.Tags.Select(t => $"{t.Key}: {string.Join(", ", t.Value)}"))}");
        }
    }
}
