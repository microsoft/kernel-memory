// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory;
using Xunit.Abstractions;

namespace FunctionalTests.Service;

public class DocumentUploadTest : BaseTestCase
{
    private readonly ISemanticMemoryClient _memory;

    public DocumentUploadTest(ITestOutputHelper output) : base(output)
    {
        this._memory = MemoryClientBuilder.BuildWebClient("http://127.0.0.1:9001/");
    }

    [Fact]
    public async Task ItUploadsPDFDocsAndDeletesAsync()
    {
        const string Id = "file1-NASA-news.pdf";

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

        var answer = await this._memory.AskAsync("What is Orion?");
        this.Log(answer.Result);
        Assert.Contains("spacecraft", answer.Result);

        this.Log("Deleting memories extracted from the document");
        await this._memory.DeleteDocumentAsync(Id);
    }
}
