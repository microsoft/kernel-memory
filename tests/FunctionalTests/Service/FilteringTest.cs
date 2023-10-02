// Copyright (c) Microsoft. All rights reserved.

// ReSharper disable InconsistentNaming

using FunctionalTests.TestHelpers;
using Microsoft.SemanticMemory;
using Xunit.Abstractions;

namespace FunctionalTests.Service;

public class FilteringTest : BaseTestCase
{
    private readonly ISemanticMemoryClient _memory;

    public FilteringTest(ITestOutputHelper output) : base(output)
    {
        this._memory = MemoryClientBuilder.BuildWebClient("http://127.0.0.1:9001/");
    }

    [Fact]
    public async Task ItSupportsFilters()
    {
        string indexName = Guid.NewGuid().ToString("D");
        const string Id = "file1-NASA-news.pdf";
        const string NotFound = "INFO NOT FOUND";
        const string Found = "spacecraft";

        this.Log("Uploading document");
        await this._memory.ImportDocumentAsync(
            new Document(Id)
                .AddFile("file1-NASA-news.pdf")
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

        // Simple filter: known user can see the memory
        answer = await this._memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("user", "admin"), index: indexName);
        this.Log(answer.Result);
        Assert.Contains(Found, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Simple filter: known user can see the memory
        answer = await this._memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("user", "owner"), index: indexName);
        this.Log(answer.Result);
        Assert.Contains(Found, answer.Result, StringComparison.OrdinalIgnoreCase);

        // Multiple filters: unknown users cannot see the memory
        answer = await this._memory.AskAsync("What is Orion?", filters: new List<MemoryFilter>
        {
            MemoryFilters.ByTag("user", "someone1"),
            MemoryFilters.ByTag("user", "someone2"),
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

        this.Log("Deleting memories extracted from the document");
        await this._memory.DeleteDocumentAsync(Id, index: indexName);
    }
}
