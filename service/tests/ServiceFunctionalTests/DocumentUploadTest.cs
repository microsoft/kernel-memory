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
}
