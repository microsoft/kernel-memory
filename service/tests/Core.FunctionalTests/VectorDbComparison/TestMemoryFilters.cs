// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.MemoryDb.AzureAISearch;
using Microsoft.KernelMemory.MemoryDb.Qdrant;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Microsoft.KernelMemory.MongoDbAtlas;
using Microsoft.KernelMemory.Postgres;
using Microsoft.KM.TestHelpers;
using Xunit.Abstractions;

// ReSharper disable MissingBlankLines

namespace Microsoft.KM.Core.FunctionalTests.VectorDbComparison;

// #pragma warning disable CS8600 // by design
// #pragma warning disable CS8604 // by design
public class TestMemoryFilters(IConfiguration cfg, ITestOutputHelper log) : BaseFunctionalTestCase(cfg, log)
{
    private const string IndexName = "test-filters";

    private readonly ITestOutputHelper _log = log;

    [Fact]
    [Trait("Category", "Serverless")]
    public async Task TestFilters()
    {
        bool azSearchEnabled = true;
        bool mongoDbAtlasEnabled = false;
        bool postgresEnabled = true;
        bool qdrantEnabled = false;

        // Booleans used for investigating test failures
        const bool DeleteIndex = true;
        const bool CreateIndex = true;
        const bool CreateRecords = true;

        var embeddingGenerator = new FakeEmbeddingGenerator();

        AzureAISearchMemory acs = null!;
        if (azSearchEnabled) { acs = new AzureAISearchMemory(this.AzureAiSearchConfig, embeddingGenerator); }

        MongoDbAtlasMemory mongoDbAtlas = null!;
        if (mongoDbAtlasEnabled) { mongoDbAtlas = new MongoDbAtlasMemory(this.MongoDbAtlasConfig, embeddingGenerator); }

        PostgresMemory postgres = null!;
        if (postgresEnabled) { postgres = new PostgresMemory(this.PostgresConfig, embeddingGenerator); }

        QdrantMemory qdrant = null!;
        if (qdrantEnabled) { qdrant = new QdrantMemory(this.QdrantConfig, embeddingGenerator); }

        var simpleVecDb = new SimpleVectorDb(this.SimpleVectorDbConfig, embeddingGenerator);

        if (DeleteIndex)
        {
            if (azSearchEnabled) { await acs.DeleteIndexAsync(IndexName); }

            if (qdrantEnabled) { await qdrant.DeleteIndexAsync(IndexName); }

            if (postgresEnabled) { await postgres.DeleteIndexAsync(IndexName); }

            if (mongoDbAtlasEnabled) { await mongoDbAtlas.DeleteIndexAsync(IndexName); }

            await simpleVecDb.DeleteIndexAsync(IndexName);

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        if (CreateIndex)
        {
            if (azSearchEnabled) { await acs.CreateIndexAsync(IndexName, 3); }

            if (qdrantEnabled) { await qdrant.CreateIndexAsync(IndexName, 3); }

            if (postgresEnabled) { await postgres.CreateIndexAsync(IndexName, 3); }

            if (mongoDbAtlasEnabled) { await mongoDbAtlas!.CreateIndexAsync(IndexName, 3); }

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
                if (azSearchEnabled) { await acs.UpsertAsync(IndexName, r.Value); }

                if (qdrantEnabled) { await qdrant.UpsertAsync(IndexName, r.Value); }

                if (postgresEnabled) { await postgres.UpsertAsync(IndexName, r.Value); }

                if (mongoDbAtlasEnabled) { await mongoDbAtlas.UpsertAsync(IndexName, r.Value); }

                await simpleVecDb.UpsertAsync(IndexName, r.Value);
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        for (int i = 1; i <= 3; i++)
        {
            if (azSearchEnabled)
            {
                this._log.WriteLine("----- Azure AI Search -----");
                await this.TestVectorDbFiltering(acs, i);
            }

            if (qdrantEnabled)
            {
                this._log.WriteLine("\n----- Qdrant vector DB -----");
                await this.TestVectorDbFiltering(qdrant, i);
            }

            if (postgresEnabled)
            {
                this._log.WriteLine("\n----- Postgres vector DB -----");
                await this.TestVectorDbFiltering(postgres, i);
            }

            if (mongoDbAtlasEnabled)
            {
                this._log.WriteLine("\n----- MongoDB Atlas vector DB -----");
                await this.TestVectorDbFiltering(mongoDbAtlas, i);
            }

            this._log.WriteLine("\n----- Simple vector DB -----");
            await this.TestVectorDbFiltering(simpleVecDb, i);

            this._log.WriteLine("\n\n");
        }
    }

    // NOTE: result order does not matter, checking result count only
    private async Task TestVectorDbFiltering(IMemoryDb vectorDb, int test)
    {
        // Single memory filter
        if (test == 1)
        {
            var singleFilter = new List<MemoryFilter> { MemoryFilters.ByTag("user", "Kaylee") };
            var singleFilterResults = await vectorDb.GetListAsync(IndexName, filters: singleFilter, limit: int.MaxValue).ToListAsync();
            this._log.WriteLine($"\nSingle memory filter: {singleFilterResults.Count} results");
            foreach (MemoryRecord r in singleFilterResults.OrderBy(x => x.Id))
            {
                this._log.WriteLine($" - ID: {r.Id}, Tags: {string.Join(", ", r.Tags.Select(t => $"{t.Key}: {string.Join(", ", t.Value)}"))}");
            }

            Assert.Equal(5, singleFilterResults.Count);
        }

        // Single memory filter with multiple tags
        if (test == 2)
        {
            var singleFilterMultipleTags = new List<MemoryFilter> { MemoryFilters.ByTag("user", "Kaylee").ByTag("collection", "Work") };
            var singleFilterMultipleTagsResults = await vectorDb.GetListAsync(IndexName, filters: singleFilterMultipleTags, limit: int.MaxValue).ToListAsync();
            this._log.WriteLine($"\nSingle memory filter with multiple tags: {singleFilterMultipleTagsResults.Count} results");
            foreach (MemoryRecord r in singleFilterMultipleTagsResults.OrderBy(x => x.Id))
            {
                this._log.WriteLine($" - ID: {r.Id}, Tags: {string.Join(", ", r.Tags.Select(t => $"{t.Key}: {string.Join(", ", t.Value)}"))}");
            }

            Assert.Equal(1, singleFilterMultipleTagsResults.Count);
        }

        // Multiple memory filters with multiple tags
        if (test == 3)
        {
            var multipleFilters = new List<MemoryFilter>
            {
                MemoryFilters.ByTag("user", "Kaylee").ByTag("collection", "Family"),
                MemoryFilters.ByTag("user", "Madelynn").ByTag("collection", "Personal")
            };
            var multipleFiltersResults = await vectorDb.GetListAsync(IndexName, filters: multipleFilters, limit: int.MaxValue).ToListAsync();
            this._log.WriteLine($"\nMultiple memory filters with multiple tags: {multipleFiltersResults.Count} results");
            foreach (MemoryRecord r in multipleFiltersResults.OrderBy(x => x.Id))
            {
                this._log.WriteLine($" - ID: {r.Id}, Tags: {string.Join(", ", r.Tags.Select(t => $"{t.Key}: {string.Join(", ", t.Value)}"))}");
            }

            Assert.Equal(4, multipleFiltersResults.Count);
        }
    }
}
