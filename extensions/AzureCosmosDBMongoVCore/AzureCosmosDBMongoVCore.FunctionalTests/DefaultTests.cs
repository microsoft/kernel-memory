// Copyright (c) Microsoft. All rights reserved.

using FunctionalTests.DefaultTestCases;
using Microsoft.KernelMemory;
using Microsoft.TestHelpers;
using Xunit.Abstractions;

namespace AzureCosmosDBMongoVCore.FunctionalTests;

public class DefaultTests : BaseFunctionalTestCase
{
    private readonly MemoryServerless _memory;

    public DefaultTests(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
        Assert.False(string.IsNullOrEmpty(this.AzureCosmosDBMongoVCoreConfig.ConnectionString));
        Assert.False(string.IsNullOrEmpty(this.AzureCosmosDBMongoVCoreConfig.IndexName));
        Assert.False(string.IsNullOrEmpty(this.AzureCosmosDBMongoVCoreConfig.Kind));
        Assert.False(string.IsNullOrEmpty(this.AzureCosmosDBMongoVCoreConfig.NumLists));
        Assert.False(string.IsNullOrEmpty(this.AzureCosmosDBMongoVCoreConfig.Similarity));
        Assert.False(string.IsNullOrEmpty(this.AzureCosmosDBMongoVCoreConfig.Dimensions));
        Assert.False(string.IsNullOrEmpty(this.AzureCosmosDBMongoVCoreConfig.NumberOfConnections));
        Assert.False(string.IsNullOrEmpty(this.AzureCosmosDBMongoVCoreConfig.EfConstruction));
        Assert.False(string.IsNullOrEmpty(this.AzureCosmosDBMongoVCoreConfig.EfSearch));
        Assert.False(string.IsNullOrEmpty(this.OpenAiConfig.APIKey));

        this._memory = new KernelMemoryBuilder()
            .WithSearchClientConfig(new SearchClientConfig { EmptyAnswer = NotFound })
            .WithOpenAI(this.OpenAiConfig)
            .WithAzureCosmosDBMongoVCoreMemoryDb(this.AzureCosmosDBMongoVCoreConfig)
            .Build<MemoryServerless>();
    }

    [Fact]
    [Trait("Category", "AzCosmosDBMongoVCore")]
    public async Task ItListsIndexes()
    {
        await IndexListTest.ItListsIndexes(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "AzCosmosDBMongoVCore")]
    public async Task ItNormalizesIndexNames()
    {
        await IndexListTest.ItNormalizesIndexNames(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "AzCosmosDBMongoVCore")]
    public async Task ItUsesDefaultIndexName()
    {
        await IndexListTest.ItUsesDefaultIndexName(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "AzCosmosDBMongoVCore")]
    public async Task ItDeletesIndexes()
    {
        await IndexDeletionTest.ItDeletesIndexes(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "AzCosmosDBMongoVCore")]
    public async Task ItHandlesMissingIndexesConsistently()
    {
        await MissingIndexTest.ItHandlesMissingIndexesConsistently(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "AzCosmosDBMongoVCore")]
    public async Task ItUploadsPDFDocsAndDeletes()
    {
        await DocumentUploadTest.ItUploadsPDFDocsAndDeletes(this._memory, this.Log);
    }
}
