﻿// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;

namespace FunctionalTests.DefaultTestCases;

public static class DocumentUploadTest
{
    public static async Task ItUploadsPDFDocsAndDeletes(IKernelMemory memory, Action<string> log)
    {
        // Arrange
        const string Id = "ItUploadsPDFDocsAndDeletes-file1-NASA-news.pdf";
        log("Uploading document");
        await memory.ImportDocumentAsync(
            "file1-NASA-news.pdf",
            documentId: Id,
            steps: Constants.PipelineWithoutSummary);

        while (!await memory.IsDocumentReadyAsync(documentId: Id))
        {
            log("Waiting for memory ingestion to complete...");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        // Act
        var answer = await memory.AskAsync("What is Orion?");
        log(answer.Result);

        // Assert
        Assert.Contains("spacecraft", answer.Result, StringComparison.OrdinalIgnoreCase);

        // Cleanup
        log("Deleting memories extracted from the document");
        await memory.DeleteDocumentAsync(Id);
    }

    public static async Task ItSupportsTags(IKernelMemory memory, Action<string> log, bool withRetries = false)
    {
        // Arrange
        const string Id = "ItSupportTags-file1-NASA-news.pdf";
        await memory.ImportDocumentAsync(
            "file1-NASA-news.pdf",
            documentId: Id,
            tags: new TagCollection
            {
                { "type", "news" },
                { "type", "test" },
                { "ext", "pdf" }
            },
            steps: Constants.PipelineWithoutSummary);

        while (!await memory.IsDocumentReadyAsync(documentId: Id))
        {
            log("Waiting for memory ingestion to complete...");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        // Act
        var retries = withRetries ? 4 : 0;
        var answer1 = await memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("type", "news"));
        log("answer1: " + answer1.Result);
        while (retries-- > 0 && !answer1.Result.Contains("spacecraft"))
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            answer1 = await memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("type", "news"));
            log("answer1: " + answer1.Result);
        }

        retries = withRetries ? 4 : 0;
        var answer2 = await memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("type", "test"));
        log("answer2: " + answer2.Result);
        while (retries-- > 0 && !answer2.Result.Contains("spacecraft"))
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            answer2 = await memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("type", "test"));
            log("answer2: " + answer2.Result);
        }

        retries = withRetries ? 4 : 0;
        var answer3 = await memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("ext", "pdf"));
        log("answer3: " + answer3.Result);
        while (retries-- > 0 && !answer3.Result.Contains("spacecraft"))
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            answer3 = await memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("type", "test"));
            log("answer3: " + answer3.Result);
        }

        var answer4 = await memory.AskAsync("What is Orion?", filter: MemoryFilters.ByTag("foo", "bar"));
        log(answer4.Result);

        // Assert
        Assert.Contains("spacecraft", answer1.Result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("spacecraft", answer2.Result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("spacecraft", answer3.Result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NOT FOUND", answer4.Result, StringComparison.OrdinalIgnoreCase);
    }
}
