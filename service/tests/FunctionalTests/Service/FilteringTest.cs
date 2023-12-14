// Copyright (c) Microsoft. All rights reserved.

using FunctionalTests.TestHelpers;
using Microsoft.KernelMemory;
using Xunit.Abstractions;

namespace FunctionalTests.Service;

public class FilteringTest : BaseTestCase
{
    private readonly IKernelMemory _memory;

    public FilteringTest(
        IConfiguration cfg,
        ITestOutputHelper output) : base(cfg, output)
    {
        this._memory = this.GetMemoryWebClient();
    }

    [Fact]
    public async Task ItSupportsASingleFilter()
    {
        string indexName = Guid.NewGuid().ToString("D");
        const string Id = "ItSupportsASingleFilter-file1-NASA-news.pdf";
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
        Assert.True(answer.NoResult);
        Assert.Contains(NotFound, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Simple filter: test AND logic: valid type + invalid user
        answer = await this._memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("type", "news").ByTag("user", "someone"), index: indexName);
        this.Log(answer.Result);
        Assert.True(answer.NoResult);
        Assert.Contains(NotFound, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Simple filter: test AND logic: invalid type + valid user
        answer = await this._memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("type", "fact").ByTag("user", "owner"), index: indexName);
        this.Log(answer.Result);
        Assert.True(answer.NoResult);
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

    [Fact]
    public async Task ItSupportsMultipleFilters()
    {
        string indexName = Guid.NewGuid().ToString("D");
        const string Id = "ItSupportsMultipleFilters-file1-NASA-news.pdf";
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

    [Fact]
    public async Task ItIgnoresEmptyFilters()
    {
        string indexName = Guid.NewGuid().ToString("D");
        const string Id = "ItIgnoresEmptyFilters-file1-NASA-news.pdf";
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
}
