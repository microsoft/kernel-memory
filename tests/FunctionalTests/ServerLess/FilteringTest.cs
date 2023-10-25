// Copyright (c) Microsoft. All rights reserved.

// ReSharper disable InconsistentNaming

using FunctionalTests.TestHelpers;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.ContentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Xunit.Abstractions;

namespace FunctionalTests.ServerLess;

public class FilteringTest : BaseTestCase
{
    private IKernelMemory? _memory = null;
    private readonly IConfiguration _cfg;

    public FilteringTest(IConfiguration cfg, ITestOutputHelper output) : base(output)
    {
        this._cfg = cfg;
    }

    [Theory]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    [InlineData("qdrant")]
    [InlineData("acs")]
    public async Task ItSupportsASingleFilter(string memoryType)
    {
        this._memory = this.GetMemory(memoryType);

        string indexName = Guid.NewGuid().ToString("D");
        const string Id = "file1-NASA-news.pdf";
        const string NotFound = "INFO NOT FOUND";
        const string Found = "spacecraft";

        this.Log("Uploading document");
        await this._memory.ImportDocumentAsync(
            new Document(Id)
                .AddFile("file1-NASA-news.pdf")
                .AddTag("type", "news")
                .AddTag("user", "admin")
                .AddTag("user", "owner"),
            index: indexName,
            steps: Constants.PipelineWithoutSummary);

        while (!await this._memory.IsDocumentReadyAsync(documentId: Id, index: indexName))
        {
            this.Log("Waiting for memory ingestion to complete...");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        // Simple filter: unknown user cannot see the memory
        var answer = await this._memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("user", "someone"), index: indexName);
        this.Log(answer.Result);
        Assert.Contains(NotFound, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Simple filter: test AND logic: valid type + invalid user
        answer = await this._memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("type", "news").ByTag("user", "someone"), index: indexName);
        this.Log(answer.Result);
        Assert.Contains(NotFound, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Simple filter: test AND logic: invalid type + valid user
        answer = await this._memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("type", "fact").ByTag("user", "owner"), index: indexName);
        this.Log(answer.Result);
        Assert.Contains(NotFound, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Simple filter: known user can see the memory
        answer = await this._memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("user", "admin"), index: indexName);
        this.Log(answer.Result);
        Assert.Contains(Found, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Simple filter: known user can see the memory
        answer = await this._memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("user", "owner"), index: indexName);
        this.Log(answer.Result);
        Assert.Contains(Found, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Simple filter: test AND logic with correct values
        answer = await this._memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("type", "news").ByTag("user", "owner"), index: indexName);
        this.Log(answer.Result);
        Assert.Contains(Found, answer.Result, StringComparison.OrdinalIgnoreCase);

        this.Log("Deleting memories extracted from the document");
        await this._memory.DeleteDocumentAsync(Id, index: indexName);
    }

    [Theory]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    [InlineData("qdrant")]
    [InlineData("acs")]
    public async Task ItSupportsMultipleFilters(string memoryType)
    {
        this._memory = this.GetMemory(memoryType);

        string indexName = Guid.NewGuid().ToString("D");
        const string Id = "file1-NASA-news.pdf";
        const string NotFound = "INFO NOT FOUND";
        const string Found = "spacecraft";

        this.Log("Uploading document");
        await this._memory.ImportDocumentAsync(
            new Document(Id)
                .AddFile("file1-NASA-news.pdf")
                .AddTag("type", "news")
                .AddTag("user", "admin")
                .AddTag("user", "owner"),
            index: indexName,
            steps: Constants.PipelineWithoutSummary);

        while (!await this._memory.IsDocumentReadyAsync(documentId: Id, index: indexName))
        {
            this.Log("Waiting for memory ingestion to complete...");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        // Multiple filters: unknown users cannot see the memory
        var answer = await this._memory.AskAsync("What is Orion?", filters: new List<MemoryFilter>
        {
            MemoryFilters.ByTag("user", "someone1"),
            MemoryFilters.ByTag("user", "someone2"),
        }, index: indexName);
        this.Log(answer.Result);
        Assert.Contains(NotFound, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Multiple filters: unknown users cannot see the memory even if the type is correct (testing AND logic)
        answer = await this._memory.AskAsync("What is Orion?", filters: new List<MemoryFilter>
        {
            MemoryFilters.ByTag("user", "someone1").ByTag("type", "news"),
            MemoryFilters.ByTag("user", "someone2").ByTag("type", "news"),
        }, index: indexName);
        this.Log(answer.Result);
        Assert.Contains(NotFound, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Multiple filters: AND + OR logic works
        answer = await this._memory.AskAsync("What is Orion?", filters: new List<MemoryFilter>
        {
            MemoryFilters.ByTag("user", "someone1").ByTag("type", "news"),
            MemoryFilters.ByTag("user", "admin").ByTag("type", "fact"),
        }, index: indexName);
        this.Log(answer.Result);
        Assert.Contains(NotFound, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Multiple filters: OR logic works
        answer = await this._memory.AskAsync("What is Orion?", filters: new List<MemoryFilter>
        {
            MemoryFilters.ByTag("user", "someone1"),
            MemoryFilters.ByTag("user", "admin"),
        }, index: indexName);
        this.Log(answer.Result);
        Assert.Contains(Found, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Multiple filters: OR logic works
        answer = await this._memory.AskAsync("What is Orion?", filters: new List<MemoryFilter>
        {
            MemoryFilters.ByTag("user", "someone1").ByTag("type", "news"),
            MemoryFilters.ByTag("user", "admin").ByTag("type", "news"),
        }, index: indexName);
        this.Log(answer.Result);
        Assert.Contains(Found, answer.Result, StringComparison.OrdinalIgnoreCase);

        this.Log("Deleting memories extracted from the document");
        await this._memory.DeleteDocumentAsync(Id, index: indexName);
    }

    [Theory]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    [InlineData("qdrant")]
    [InlineData("acs")]
    public async Task ItIgnoresEmptyFilters(string memoryType)
    {
        this._memory = this.GetMemory(memoryType);

        string indexName = Guid.NewGuid().ToString("D");
        const string Id = "file1-NASA-news.pdf";
        const string Found = "spacecraft";

        this.Log("Uploading document");
        await this._memory.ImportDocumentAsync(
            new Document(Id)
                .AddFile("file1-NASA-news.pdf")
                .AddTag("type", "news")
                .AddTag("user", "admin")
                .AddTag("user", "owner"),
            index: indexName,
            steps: Constants.PipelineWithoutSummary);

        while (!await this._memory.IsDocumentReadyAsync(documentId: Id, index: indexName))
        {
            this.Log("Waiting for memory ingestion to complete...");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        // Simple filter: empty filters have no impact
        var answer = await this._memory.AskAsync("What is Orion?", filter: new(), index: indexName);
        this.Log(answer.Result);
        Assert.Contains(Found, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Multiple filters: empty filters have no impact
        answer = await this._memory.AskAsync("What is Orion?", filters: new List<MemoryFilter> { new() }, index: indexName);
        this.Log(answer.Result);
        Assert.Contains(Found, answer.Result, StringComparison.OrdinalIgnoreCase);

        this.Log("Deleting memories extracted from the document");
        await this._memory.DeleteDocumentAsync(Id, index: indexName);
    }

    private IKernelMemory GetMemory(string memoryType)
    {
        var openAIKey = Env.Var("OPENAI_API_KEY");

        switch (memoryType)
        {
            case "default":
                return new KernelMemoryBuilder()
                    .WithOpenAIDefaults(openAIKey)
                    .BuildServerlessClient();

            case "simple_on_disk":
                return new KernelMemoryBuilder()
                    .WithOpenAIDefaults(openAIKey)
                    .WithSimpleVectorDb(new SimpleVectorDbConfig { Directory = "_vectors", StorageType = FileSystemTypes.Disk })
                    .WithSimpleFileStorage(new SimpleFileStorageConfig { Directory = "_files", StorageType = FileSystemTypes.Disk })
                    .BuildServerlessClient();

            case "simple_volatile":
                return new KernelMemoryBuilder()
                    .WithOpenAIDefaults(openAIKey)
                    .WithSimpleVectorDb(new SimpleVectorDbConfig { StorageType = FileSystemTypes.Volatile })
                    .WithSimpleFileStorage(new SimpleFileStorageConfig { StorageType = FileSystemTypes.Volatile })
                    .BuildServerlessClient();

            case "qdrant":
                var qdrantEndpoint = this._cfg.GetSection("Services").GetSection("Qdrant").GetValue<string>("Endpoint");
                Assert.False(string.IsNullOrEmpty(qdrantEndpoint));
                return new KernelMemoryBuilder()
                    .WithOpenAIDefaults(openAIKey)
                    .WithQdrant(qdrantEndpoint)
                    .BuildServerlessClient();

            case "acs":
                var acsEndpoint = this._cfg.GetSection("Services").GetSection("AzureCognitiveSearch").GetValue<string>("Endpoint");
                var acsKey = this._cfg.GetSection("Services").GetSection("AzureCognitiveSearch").GetValue<string>("APIKey");
                Assert.False(string.IsNullOrEmpty(acsEndpoint));
                Assert.False(string.IsNullOrEmpty(acsKey));
                return new KernelMemoryBuilder()
                    .WithOpenAIDefaults(openAIKey)
                    .WithAzureCognitiveSearch(acsEndpoint, acsKey)
                    .BuildServerlessClient();

            default:
                throw new ArgumentOutOfRangeException($"{memoryType} not supported");
        }
    }
}
