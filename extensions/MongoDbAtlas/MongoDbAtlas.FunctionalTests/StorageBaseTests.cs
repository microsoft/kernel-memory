// Copyright (c) Microsoft. All rights reserved.

using System.Text;
using Microsoft.KernelMemory.ContentStorage;
using Microsoft.KernelMemory.MongoDbAtlas;
using Microsoft.KernelMemory.MongoDbAtlas.Helpers;

namespace MongoDbAtlas.FunctionalTests;

public abstract class StorageBaseTests : IDisposable
{
    private readonly IConfiguration _cfg;
    private readonly ITestOutputHelper _output;
    private readonly MongoDbKernelMemoryConfiguration _mongoDbAtlasMemoryConfiguration;
    private readonly MongoDbKernelMemoryStorage _sut;

    private readonly string IndexName = $"storagetestindex{_seed++}";

    private static int _seed = 0;

    protected StorageBaseTests(IConfiguration cfg, ITestOutputHelper output)
    {
        this._cfg = cfg;
        this._output = output;

        this._mongoDbAtlasMemoryConfiguration = this.GetConfiguration(cfg);
        var ash = new AtlasSearchHelper(this._mongoDbAtlasMemoryConfiguration.ConnectionString, this._mongoDbAtlasMemoryConfiguration.DatabaseName);

        //delete everything for every collection
        ash.DropAllDocumentsFromCollectionsAsync().Wait();

        this._sut = new MongoDbKernelMemoryStorage(this._mongoDbAtlasMemoryConfiguration);
        this._sut.CreateIndexDirectoryAsync("testindex").Wait();
    }

    protected abstract MongoDbKernelMemoryConfiguration GetConfiguration(IConfiguration cfg);

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._sut.DeleteIndexDirectoryAsync(this.IndexName).Wait();
        }
    }

    [Fact]
    [Trait("Category", "MongoDbAtlas")]
    public async Task SaveFilesHonorsId()
    {
        //Act save a file with the same id updating content.
        var fileContent1 = new MemoryStream(Encoding.UTF8.GetBytes("Hello World"));
        var fileContent2 = new MemoryStream(Encoding.UTF8.GetBytes("Hello World 2"));
        string id = $"_pipeline_status{_seed++}.txt";
        await this._sut.WriteFileAsync(this.IndexName, id, "filename.txt", fileContent1);
        await this._sut.WriteFileAsync(this.IndexName, id, "filename.txt", fileContent2);

        //assert
        var file = await this._sut.ReadFileAsync(this.IndexName, id, "filename.txt");
        var content = file.ToString();

        Assert.Equal("Hello World 2", content);
    }

    [Theory]
    [Trait("Category", "MongoDbAtlas")]
    [InlineData("txt", "Hello World", "Hello world 2")]
    [InlineData("text_embedding", @"{ ""Text"": ""Hello World"" }", @"{ ""Text"": ""Hello World 2"" }")]
    public async Task SaveDifferentFiles(string extension, string content1, string content2)
    {
        //Act save a file with the same id updating content.
        var fileContent1 = new MemoryStream(Encoding.UTF8.GetBytes(content1));
        var fileContent2 = new MemoryStream(Encoding.UTF8.GetBytes(content2));
        string id = $"_pipeline_status{_seed++}.{extension}";
        string fileName1 = $"filename{_seed++}.{extension}";
        string fileName2 = $"filename{_seed++}.{extension}";

        await this._sut.WriteFileAsync(this.IndexName, id, fileName1, fileContent1);
        await this._sut.WriteFileAsync(this.IndexName, id, fileName2, fileContent2);

        //assert
        var file = await this._sut.ReadFileAsync(this.IndexName, id, fileName1);
        var content = file.ToString();

        Assert.Equal(content1, content);

        file = await this._sut.ReadFileAsync(this.IndexName, id, fileName2);
        content = file.ToString();
        Assert.Equal(content2, content);
    }

    [Fact]
    [Trait("Category", "MongoDbAtlas")]
    public async Task SaveFilesHonorsIdWithBinaryContent()
    {
        //Act save a file with the same id updating content.
        var fileContent1 = new MemoryStream(Encoding.UTF8.GetBytes("Hello World"));
        var fileContent2 = new MemoryStream(Encoding.UTF8.GetBytes("Hello World 2"));
        string id = $"_pipeline_status{_seed++}.txt";
        var fileName = $"filename{_seed++}.bin";
        await this._sut.WriteFileAsync(this.IndexName, id, fileName, fileContent1);
        await this._sut.WriteFileAsync(this.IndexName, id, fileName, fileContent2);

        //assert
        var file = await this._sut.ReadFileAsync(this.IndexName, id, fileName);
        var content = file.ToString();

        Assert.Equal("Hello World 2", content);
    }

    [Fact]
    [Trait("Category", "MongoDbAtlas")]
    public async Task SaveDifferentFilesWithBinaryContent()
    {
        //Act save a file with the same id updating content.
        var fileContent1 = new MemoryStream(Encoding.UTF8.GetBytes("Hello World"));
        var fileContent2 = new MemoryStream(Encoding.UTF8.GetBytes("Hello World 2"));
        string id = $"_pipeline_status{_seed++}.txt";
        var fileName1 = $"filename{_seed++}.bin";
        var fileName2 = $"filename{_seed++}.bin";
        await this._sut.WriteFileAsync(this.IndexName, id, fileName1, fileContent1);
        await this._sut.WriteFileAsync(this.IndexName, id, fileName2, fileContent2);

        //assert
        var file = await this._sut.ReadFileAsync(this.IndexName, id, fileName1);
        var content = file.ToString();

        Assert.Equal("Hello World", content);

        file = await this._sut.ReadFileAsync(this.IndexName, id, fileName2);
        content = file.ToString();
        Assert.Equal("Hello World 2", content);
    }

    [Fact]
    [Trait("Category", "MongoDbAtlas")]
    public async Task CanCleanIndexCorrectly()
    {
        //Arrange: save some files into the index
        var fileContent1 = new MemoryStream(Encoding.UTF8.GetBytes("Hello World"));
        var fileContent2 = new MemoryStream(Encoding.UTF8.GetBytes("Hello World 2"));
        string id = $"_pipeline_status{_seed++}.txt";
        var fileName1 = $"filename{_seed++}.txt";
        var fileName2 = $"filename{_seed++}.bin";
        await this._sut.WriteFileAsync(this.IndexName, id, fileName1, fileContent1);
        await this._sut.WriteFileAsync(this.IndexName, id, fileName2, fileContent2);

        //Act: clean the index
        await this._sut.EmptyDocumentDirectoryAsync(this.IndexName, id);

        //Assert: check that the files are not there anymore
        await Assert.ThrowsAsync<ContentStorageFileNotFoundException>(async () => await this._sut.ReadFileAsync(this.IndexName, id, fileName1, false));
        await Assert.ThrowsAsync<ContentStorageFileNotFoundException>(async () => await this._sut.ReadFileAsync(this.IndexName, id, fileName2, false));
    }
}

public class SingleCollectionBaseTests : StorageBaseTests
{
    public SingleCollectionBaseTests(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
    }

    protected override MongoDbKernelMemoryConfiguration GetConfiguration(IConfiguration cfg)
    {
        var config = cfg.GetSection("KernelMemory:Services:MongoDb").Get<MongoDbKernelMemoryConfiguration>()!;
        config.DatabaseName += "StorageTests";
        config.UseSingleCollectionForVectorSearch = true;
        return config;
    }
}

public class MultipleCollectionBaseTests : StorageBaseTests
{
    public MultipleCollectionBaseTests(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
    }

    protected override MongoDbKernelMemoryConfiguration GetConfiguration(IConfiguration cfg)
    {
        var config = cfg.GetSection("KernelMemory:Services:MongoDb").Get<MongoDbKernelMemoryConfiguration>()!;
        config.DatabaseName += "StorageTests";
        config.UseSingleCollectionForVectorSearch = false;
        return config;
    }
}
