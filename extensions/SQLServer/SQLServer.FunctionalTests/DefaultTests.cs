// Copyright (c) Microsoft. All rights reserved.

// When using KernelMemoryDev there are two references to Abstractions (project + package)
// because the SqlServer extension is available only as a package, which includes a reference to Abstraction package.
// As a result, the compiler is unable to see either the Abstractions, with a build error, so we allow these
// tests only when working with packages.

#if !KernelMemoryDev
using FunctionalTests.DefaultTestCases;
using KernelMemory.MemoryStorage.SqlServer;
using Microsoft.KernelMemory;
using Microsoft.TestHelpers;

namespace SQLServer.FunctionalTests;

public class DefaultTests : BaseFunctionalTestCase
{
    private readonly MemoryServerless _memory;
    private SqlServerConfig _sqlServerConfig;

    public DefaultTests(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
        Assert.False(string.IsNullOrEmpty(this.OpenAiConfig.APIKey));

        this._sqlServerConfig = cfg.GetSection("Services:SqlServer").Get<SqlServerConfig>()!;

        this._memory = new KernelMemoryBuilder()
            .WithSearchClientConfig(new SearchClientConfig { EmptyAnswer = NotFound })
            .WithOpenAI(this.OpenAiConfig)
            // .WithAzureOpenAITextGeneration(this.AzureOpenAITextConfiguration)
            // .WithAzureOpenAITextEmbeddingGeneration(this.AzureOpenAIEmbeddingConfiguration)
            .WithSqlServerMemoryDb(this._sqlServerConfig)
            .Build<MemoryServerless>();
    }

    [Fact]
    [Trait("Category", "SQLServer")]
    public async Task ItSupportsASingleFilter()
    {
        await FilteringTest.ItSupportsASingleFilter(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "SQLServer")]
    public async Task ItSupportsMultipleFilters()
    {
        await FilteringTest.ItSupportsMultipleFilters(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "SQLServer")]
    public async Task ItIgnoresEmptyFilters()
    {
        await FilteringTest.ItIgnoresEmptyFilters(this._memory, this.Log, true);
    }

    [Fact]
    [Trait("Category", "SQLServer")]
    public async Task ItListsIndexes()
    {
        await IndexListTest.ItListsIndexes(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "SQLServer")]
    public async Task ItNormalizesIndexNames()
    {
        await IndexListTest.ItNormalizesIndexNames(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "SQLServer")]
    public async Task ItDeletesIndexes()
    {
        await IndexDeletionTest.ItDeletesIndexes(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "SQLServer")]
    public async Task ItHandlesMissingIndexesConsistently()
    {
        await MissingIndexTest.ItHandlesMissingIndexesConsistently(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "SQLServer")]
    public async Task ItUploadsPDFDocsAndDeletes()
    {
        await DocumentUploadTest.ItUploadsPDFDocsAndDeletes(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "SQLServer")]
    public async Task ItSupportsTags()
    {
        await DocumentUploadTest.ItSupportsTags(this._memory, this.Log);
    }
}

#endif
