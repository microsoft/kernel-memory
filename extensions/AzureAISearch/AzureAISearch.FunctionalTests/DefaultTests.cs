// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;
using Microsoft.KM.Core.FunctionalTests.DefaultTestCases;
using Microsoft.KM.TestHelpers;

namespace Microsoft.AzureAISearch.FunctionalTests;

public class DefaultTests : BaseFunctionalTestCase
{
    private readonly MemoryServerless _memory;

    public DefaultTests(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
        Assert.False(string.IsNullOrEmpty(this.AzureAiSearchConfig.Endpoint));
        Assert.False(this.AzureAiSearchConfig.Auth == AzureAISearchConfig.AuthTypes.APIKey && string.IsNullOrEmpty(this.AzureAiSearchConfig.APIKey));
        Assert.False(string.IsNullOrEmpty(this.OpenAiConfig.APIKey));

        this._memory = new KernelMemoryBuilder()
            .With(new KernelMemoryConfig { DefaultIndexName = "default4tests" })
            .WithSearchClientConfig(new SearchClientConfig { EmptyAnswer = NotFound })
            .WithOpenAI(this.OpenAiConfig)
            .WithAzureAISearchMemoryDb(this.AzureAiSearchConfig)
            .Build<MemoryServerless>();
    }

    [Fact]
    [Trait("Category", "AzAISearch")]
    public async Task ItSupportsASingleFilter()
    {
        await FilteringTest.ItSupportsASingleFilter(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "AzAISearch")]
    public async Task ItSupportsMultipleFilters()
    {
        await FilteringTest.ItSupportsMultipleFilters(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "AzAISearch")]
    public async Task ItIgnoresEmptyFilters()
    {
        await FilteringTest.ItIgnoresEmptyFilters(this._memory, this.Log, true);
    }

    [Fact]
    [Trait("Category", "AzAISearch")]
    public async Task ItDoesntFailIfTheIndexExistsAlready()
    {
        await IndexCreationTest.ItDoesntFailIfTheIndexExistsAlready(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "AzAISearch")]
    public async Task ItListsIndexes()
    {
        await IndexListTest.ItListsIndexes(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "AzAISearch")]
    public async Task ItNormalizesIndexNames()
    {
        await IndexListTest.ItNormalizesIndexNames(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "AzAISearch")]
    public async Task ItUsesDefaultIndexName()
    {
        await IndexListTest.ItUsesDefaultIndexName(this._memory, this.Log, "default4tests");
    }

    [Fact]
    [Trait("Category", "AzAISearch")]
    public async Task ItDeletesIndexes()
    {
        await IndexDeletionTest.ItDeletesIndexes(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "AzAISearch")]
    public async Task ItHandlesMissingIndexesConsistently()
    {
        await MissingIndexTest.ItHandlesMissingIndexesConsistently(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "AzAISearch")]
    public async Task ItUploadsPDFDocsAndDeletes()
    {
        await DocumentUploadTest.ItUploadsPDFDocsAndDeletes(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "AzAISearch")]
    public async Task ItSupportsTags()
    {
        await DocumentUploadTest.ItSupportsTags(this._memory, this.Log, true);
    }

    [Fact]
    [Trait("Category", "AzAISearch")]
    public async Task ItDownloadsPDFDocs()
    {
        await DocumentUploadTest.ItDownloadsPDFDocs(this._memory, this.Log);
    }
}
