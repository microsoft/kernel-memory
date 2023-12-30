// Copyright (c) Microsoft. All rights reserved.

using FunctionalTests.TestHelpers;
using Microsoft.KernelMemory;
using Xunit.Abstractions;

namespace FunctionalTests.Service;

public class DocumentUploadTest : BaseTestCase
{
    private readonly IKernelMemory _memory;

    public DocumentUploadTest(
        IConfiguration cfg,
        ITestOutputHelper output) : base(cfg, output)
    {
        this._memory = this.GetMemoryWebClient();
    }

    [Fact]
    [Trait("Category", "ServiceFunctionalTest")]
    public async Task ItUploadsPDFDocsAndDeletes()
    {
        // Arrange
        const string Id = "ItUploadsPDFDocsAndDeletes-file1-NASA-news.pdf";
        this.Log("Uploading document");
        await this._memory.ImportDocumentAsync(
            "file1-NASA-news.pdf",
            documentId: Id,
            steps: Constants.PipelineWithoutSummary);

        while (!await this._memory.IsDocumentReadyAsync(documentId: Id))
        {
            this.Log("Waiting for memory ingestion to complete...");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        // Act
        var answer = await this._memory.AskAsync("What is Orion?");
        this.Log(answer.Result);

        // Assert
        Assert.Contains("spacecraft", answer.Result, StringComparison.OrdinalIgnoreCase);

        // Cleanup
        this.Log("Deleting memories extracted from the document");
        await this._memory.DeleteDocumentAsync(Id);
    }

    [Fact]
    [Trait("Category", "ServiceFunctionalTest")]
    public async Task ItSupportTags()
    {
        // Arrange
        const string Id = "ItSupportTags-file1-NASA-news.pdf";
        await this._memory.ImportDocumentAsync(
            "file1-NASA-news.pdf",
            documentId: Id,
            tags: new TagCollection
            {
                { "type", "news" },
                { "type", "test" },
                { "ext", "pdf" }
            },
            steps: Constants.PipelineWithoutSummary);

        // Act
        var answer1 = await this._memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("type", "news"));
        this.Log(answer1.Result);
        var answer2 = await this._memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("type", "test"));
        this.Log(answer2.Result);
        var answer3 = await this._memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("ext", "pdf"));
        this.Log(answer3.Result);
        var answer4 = await this._memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("foo", "bar"));
        this.Log(answer4.Result);

        // Assert
        Assert.Contains("spacecraft", answer1.Result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("spacecraft", answer2.Result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("spacecraft", answer3.Result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NOT FOUND", answer4.Result, StringComparison.OrdinalIgnoreCase);
    }
}
