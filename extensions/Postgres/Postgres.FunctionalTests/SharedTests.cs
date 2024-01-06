// Copyright (c) Microsoft. All rights reserved.

using FunctionalTests.Scenarios;
using Microsoft.KernelMemory;
using Microsoft.TestHelpers;

namespace Postgres.FunctionalTests;

public class SharedTests : BaseFunctionalTestCase
{
    private readonly MemoryServerless _memory;

    public SharedTests(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
        Assert.False(string.IsNullOrEmpty(this.OpenAiConfig.APIKey));

        this._memory = new KernelMemoryBuilder()
            .WithSearchClientConfig(new SearchClientConfig { EmptyAnswer = NotFound })
            .WithOpenAIDefaults(this.OpenAiConfig.APIKey)
            // .WithAzureOpenAITextGeneration(this.AzureOpenAITextConfiguration)
            // .WithAzureOpenAITextEmbeddingGeneration(this.AzureOpenAIEmbeddingConfiguration)
            .WithPostgresMemoryDb(this.PostgresConfig)
            .Build<MemoryServerless>();
    }

    [Fact]
    [Trait("Category", "Postgres")]
    public async Task ItSupportsASingleFilter()
    {
        await SharedFilteringTest.ItSupportsASingleFilter(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "Postgres")]
    public async Task ItSupportsMultipleFilters()
    {
        await SharedFilteringTest.ItSupportsMultipleFilters(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "Postgres")]
    public async Task ItIgnoresEmptyFilters()
    {
        await SharedFilteringTest.ItIgnoresEmptyFilters(this._memory, this.Log, true);
    }

    [Fact]
    [Trait("Category", "Postgres")]
    public async Task ItListsIndexes()
    {
        await SharedIndexListTest.ItListsIndexes(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "Postgres")]
    public async Task ItNormalizesIndexNames()
    {
        await SharedIndexListTest.ItNormalizesIndexNames(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "Postgres")]
    public async Task ItDeletesIndexes()
    {
        await SharedIndexDeletionTest.ItDeletesIndexes(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "Postgres")]
    public async Task ItHandlesMissingIndexesConsistently()
    {
        await SharedMissingIndexTest.ItHandlesMissingIndexesConsistently(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "Postgres")]
    public async Task ItUploadsPDFDocsAndDeletes()
    {
        await SharedDocumentUploadTest.ItUploadsPDFDocsAndDeletes(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "Postgres")]
    public async Task ItSupportsTags()
    {
        await SharedDocumentUploadTest.ItSupportsTags(this._memory, this.Log);
    }
}
