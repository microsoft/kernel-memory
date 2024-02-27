// Copyright (c) Microsoft. All rights reserved.

using FunctionalTests.DefaultTestCases;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.MongoDbAtlas;
using Microsoft.KernelMemory.MongoDbAtlas.Helpers;
using Microsoft.TestHelpers;

namespace MongoDbAtlas.FunctionalTests;

public class DefaultTests : BaseFunctionalTestCase
{
    private readonly MemoryServerless _memory;
    private readonly MongoDbKernelMemoryConfiguration _atlasMongoDbMemoryConfiguration;

    public DefaultTests(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
        Assert.False(string.IsNullOrEmpty(this.OpenAiConfig.APIKey));

        this._atlasMongoDbMemoryConfiguration = cfg.GetSection("KernelMemory:Services:MongoDb").Get<MongoDbKernelMemoryConfiguration>()!;

        this._atlasMongoDbMemoryConfiguration
            .WithSingleCollectionForVectorSearch(true)
            //Need to wait for atlas to grab the data from the collection and index.
            .WithAfterIndexCallback(async () => await Task.Delay(2000));

        //Clear all content in any collection before running the test.
        var ash = new AtlasSearchHelper(this._atlasMongoDbMemoryConfiguration.ConnectionString, this._atlasMongoDbMemoryConfiguration.DatabaseName);
        if (this._atlasMongoDbMemoryConfiguration.UseSingleCollectionForVectorSearch)
        {
            //delete everything for every collection
            ash.DropAllDocumentsFromCollectionsAsync().Wait();
        }
        else
        {
            //drop the entire db to be sure we can start with a clean test.
            ash.DropDatabaseAsync().Wait();
        }

        this._memory = new KernelMemoryBuilder()
            .WithSearchClientConfig(new SearchClientConfig { EmptyAnswer = NotFound })
            .WithOpenAI(this.OpenAiConfig)
            // .WithAzureOpenAITextGeneration(this.AzureOpenAITextConfiguration)
            // .WithAzureOpenAITextEmbeddingGeneration(this.AzureOpenAIEmbeddingConfiguration)
            .WithMongoDbAtlasMemoryDb(this._atlasMongoDbMemoryConfiguration)
            .Build<MemoryServerless>();
    }

    [Fact]
    [Trait("Category", "MongoDbAtlas")]
    public async Task ItSupportsASingleFilter()
    {
        await FilteringTest.ItSupportsASingleFilter(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "MongoDbAtlas")]
    public async Task ItSupportsMultipleFilters()
    {
        await FilteringTest.ItSupportsMultipleFilters(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "MongoDbAtlas")]
    public async Task ItIgnoresEmptyFilters()
    {
        await FilteringTest.ItIgnoresEmptyFilters(this._memory, this.Log, true);
    }

    [Fact]
    [Trait("Category", "MongoDbAtlas")]
    public async Task ItListsIndexes()
    {
        await IndexListTest.ItListsIndexes(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "MongoDbAtlas")]
    public async Task ItNormalizesIndexNames()
    {
        await IndexListTest.ItNormalizesIndexNames(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "MongoDbAtlas")]
    public async Task ItDeletesIndexes()
    {
        await IndexDeletionTest.ItDeletesIndexes(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "MongoDbAtlas")]
    public async Task ItHandlesMissingIndexesConsistently()
    {
        await MissingIndexTest.ItHandlesMissingIndexesConsistently(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "MongoDbAtlas")]
    public async Task ItUploadsPDFDocsAndDeletes()
    {
        await DocumentUploadTest.ItUploadsPDFDocsAndDeletes(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "MongoDbAtlas")]
    public async Task ItSupportsTags()
    {
        await DocumentUploadTest.ItSupportsTags(this._memory, this.Log);
    }
}
