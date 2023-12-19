// Copyright (c) Microsoft. All rights reserved.

using FunctionalTests.TestHelpers;
using Microsoft.KernelMemory;
using Xunit.Abstractions;

namespace FunctionalTests.ServerLess;

public class FilteringTest : BaseTestCase
{
    public FilteringTest(
        IConfiguration cfg,
        ITestOutputHelper output) : base(cfg, output)
    {
    }

    [Theory]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    [InlineData("qdrant")]
    [InlineData("az_ai_search")]
    public async Task ItSupportsASingleFilter(string memoryType)
    {
        var memory = this.GetServerlessMemory(memoryType);

        string indexName = Guid.NewGuid().ToString("D");
        const string Id = "file1-NASA-news.pdf";
        const string Found = "spacecraft";

        this.Log("Uploading document");
        await memory.ImportDocumentAsync(
            new Document(Id)
                .AddFile("file1-NASA-news.pdf")
                .AddTag("type", "news")
                .AddTag("user", "admin")
                .AddTag("user", "owner"),
            index: indexName,
            steps: Constants.PipelineWithoutSummary);

        while (!await memory.IsDocumentReadyAsync(documentId: Id, index: indexName))
        {
            this.Log("Waiting for memory ingestion to complete...");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        // Simple filter: unknown user cannot see the memory
        var answer = await memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("user", "someone"), index: indexName);
        this.Log(answer.Result);
        Assert.Contains(NotFound, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Simple filter: test AND logic: valid type + invalid user
        answer = await memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("type", "news").ByTag("user", "someone"), index: indexName);
        this.Log(answer.Result);
        Assert.Contains(NotFound, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Simple filter: test AND logic: invalid type + valid user
        answer = await memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("type", "fact").ByTag("user", "owner"), index: indexName);
        this.Log(answer.Result);
        Assert.Contains(NotFound, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Simple filter: known user can see the memory
        answer = await memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("user", "admin"), index: indexName);
        this.Log(answer.Result);
        Assert.Contains(Found, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Simple filter: known user can see the memory
        answer = await memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("user", "owner"), index: indexName);
        this.Log(answer.Result);
        Assert.Contains(Found, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Simple filter: test AND logic with correct values
        answer = await memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("type", "news").ByTag("user", "owner"), index: indexName);
        this.Log(answer.Result);
        Assert.Contains(Found, answer.Result, StringComparison.OrdinalIgnoreCase);

        this.Log("Deleting memories extracted from the document");
        await memory.DeleteDocumentAsync(Id, index: indexName);

        this.Log("Deleting index");
        await memory.DeleteIndexAsync(indexName);
    }

    [Theory]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    [InlineData("qdrant")]
    [InlineData("az_ai_search")]
    public async Task ItSupportsMultipleFilters(string memoryType)
    {
        var memory = this.GetServerlessMemory(memoryType);

        string indexName = Guid.NewGuid().ToString("D");
        const string Id = "file1-NASA-news.pdf";
        const string Found = "spacecraft";

        this.Log("Uploading document");
        await memory.ImportDocumentAsync(
            new Document(Id)
                .AddFile("file1-NASA-news.pdf")
                .AddTag("type", "news")
                .AddTag("user", "admin")
                .AddTag("user", "owner"),
            index: indexName,
            steps: Constants.PipelineWithoutSummary);

        while (!await memory.IsDocumentReadyAsync(documentId: Id, index: indexName))
        {
            this.Log("Waiting for memory ingestion to complete...");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        // Multiple filters: unknown users cannot see the memory
        var answer = await memory.AskAsync("What is Orion?", filters: new List<MemoryFilter>
        {
            MemoryFilters.ByTag("user", "someone1"),
            MemoryFilters.ByTag("user", "someone2"),
        }, index: indexName);
        this.Log(answer.Result);
        Assert.Contains(NotFound, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Multiple filters: unknown users cannot see the memory even if the type is correct (testing AND logic)
        answer = await memory.AskAsync("What is Orion?", filters: new List<MemoryFilter>
        {
            MemoryFilters.ByTag("user", "someone1").ByTag("type", "news"),
            MemoryFilters.ByTag("user", "someone2").ByTag("type", "news"),
        }, index: indexName);
        this.Log(answer.Result);
        Assert.Contains(NotFound, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Multiple filters: AND + OR logic works
        answer = await memory.AskAsync("What is Orion?", filters: new List<MemoryFilter>
        {
            MemoryFilters.ByTag("user", "someone1").ByTag("type", "news"),
            MemoryFilters.ByTag("user", "admin").ByTag("type", "fact"),
        }, index: indexName);
        this.Log(answer.Result);
        Assert.Contains(NotFound, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Multiple filters: OR logic works
        answer = await memory.AskAsync("What is Orion?", filters: new List<MemoryFilter>
        {
            MemoryFilters.ByTag("user", "someone1"),
            MemoryFilters.ByTag("user", "admin"),
        }, index: indexName);
        this.Log(answer.Result);
        Assert.Contains(Found, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Multiple filters: OR logic works
        answer = await memory.AskAsync("What is Orion?", filters: new List<MemoryFilter>
        {
            MemoryFilters.ByTag("user", "someone1").ByTag("type", "news"),
            MemoryFilters.ByTag("user", "admin").ByTag("type", "news"),
        }, index: indexName);
        this.Log(answer.Result);
        Assert.Contains(Found, answer.Result, StringComparison.OrdinalIgnoreCase);

        this.Log("Deleting memories extracted from the document");
        await memory.DeleteDocumentAsync(Id, index: indexName);

        this.Log("Deleting index");
        await memory.DeleteIndexAsync(indexName);
    }

    [Theory]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    [InlineData("qdrant")]
    [InlineData("az_ai_search")]
    public async Task ItIgnoresEmptyFilters(string memoryType)
    {
        // Arrange
        var memory = this.GetServerlessMemory(memoryType);

        string indexName = Guid.NewGuid().ToString("D");
        const string Id = "file1-NASA-news.pdf";
        const string Found = "spacecraft";

        this.Log("Uploading document");
        await memory.ImportDocumentAsync(
            new Document(Id)
                .AddFile("file1-NASA-news.pdf")
                .AddTag("type", "news")
                .AddTag("user", "admin")
                .AddTag("user", "owner"),
            index: indexName,
            steps: Constants.PipelineWithoutSummary);

        while (!await memory.IsDocumentReadyAsync(documentId: Id, index: indexName))
        {
            this.Log("Waiting for memory ingestion to complete...");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        const string Question = "What is Orion?";

        // Act
        // Simple filter: empty filters have no impact
        var answer = await memory.AskAsync(Question, filter: new(), index: indexName);

        // Retry for Azure AI Search, to let the index populate
        for (int i = 0; i < 4; i++)
        {
            if (memoryType == "az_ai_search" && !answer.Result.Contains(Found, StringComparison.OrdinalIgnoreCase))
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                answer = await memory.AskAsync(Question, filter: new(), index: indexName);
            }
        }

        // Assert
        this.Log(answer.Result);
        Assert.Contains(Found, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Act
        // Multiple filters: empty filters have no impact
        answer = await memory.AskAsync(Question,
            filters: new List<MemoryFilter> { new() }, index: indexName);
        this.Log(answer.Result);

        // Assert
        Assert.Contains(Found, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Clean up
        this.Log("Deleting memories extracted from the document");
        await memory.DeleteDocumentAsync(Id, index: indexName);

        this.Log("Deleting index");
        await memory.DeleteIndexAsync(indexName);
    }
}
