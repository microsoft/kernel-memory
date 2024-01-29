// Copyright (c) Microsoft. All rights reserved.

// When using KernelMemoryDev there are two references to Abstractions (project + package)
// because the Elasticsearch extension is available only as a package, which includes a reference to Abstraction package.
// As a result, the compiler is unable to see either the Abstractions, with a build error, so we allow these
// tests only when working with packages.

#if !KernelMemoryDev
using FreeMindLabs.KernelMemory.Elasticsearch;
using FunctionalTests.DefaultTestCases;
using Microsoft.KernelMemory;
using Microsoft.TestHelpers;

namespace Elasticsearch.FunctionalTests;

public class DefaultTests : BaseFunctionalTestCase
{
    private readonly MemoryServerless _memory;
    private readonly ElasticsearchConfig _elasticsearchConfig;

    public DefaultTests(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
        Assert.False(string.IsNullOrEmpty(this.OpenAiConfig.APIKey));

        this._elasticsearchConfig = cfg.GetSection("Services:Elasticsearch").Get<ElasticsearchConfig>()!;

        this._memory = new KernelMemoryBuilder()
            .WithSearchClientConfig(new SearchClientConfig { EmptyAnswer = NotFound })
            .WithOpenAI(this.OpenAiConfig)
            // .WithAzureOpenAITextGeneration(this.AzureOpenAITextConfiguration)
            // .WithAzureOpenAITextEmbeddingGeneration(this.AzureOpenAIEmbeddingConfiguration)
            .WithElasticsearch(this._elasticsearchConfig)
            .Build<MemoryServerless>();
    }

    [Fact]
    [Trait("Category", "Elasticsearch")]
    public async Task ItSupportsASingleFilter()
    {
        await FilteringTest.ItSupportsASingleFilter(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "Elasticsearch")]
    public async Task ItSupportsMultipleFilters()
    {
        await FilteringTest.ItSupportsMultipleFilters(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "Elasticsearch")]
    public async Task ItIgnoresEmptyFilters()
    {
        await FilteringTest.ItIgnoresEmptyFilters(this._memory, this.Log, true);
    }

    [Fact]
    [Trait("Category", "Elasticsearch")]
    public async Task ItListsIndexes()
    {
        await IndexListTest.ItListsIndexes(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "Elasticsearch")]
    public async Task ItNormalizesIndexNames()
    {
        await IndexListTest.ItNormalizesIndexNames(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "Elasticsearch")]
    public async Task ItDeletesIndexes()
    {
        await IndexDeletionTest.ItDeletesIndexes(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "Elasticsearch")]
    public async Task ItHandlesMissingIndexesConsistently()
    {
        await MissingIndexTest.ItHandlesMissingIndexesConsistently(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "Elasticsearch")]
    public async Task ItUploadsPDFDocsAndDeletes()
    {
        await DocumentUploadTest.ItUploadsPDFDocsAndDeletes(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "Elasticsearch")]
    public async Task ItSupportsTags()
    {
        await DocumentUploadTest.ItSupportsTags(this._memory, this.Log);
    }
}

#endif

