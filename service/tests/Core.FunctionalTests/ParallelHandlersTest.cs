// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Microsoft.KM.TestHelpers;
using Xunit.Abstractions;

namespace Microsoft.KM.Core.FunctionalTests;

public class ParallelHandlersTest : BaseFunctionalTestCase
{
    private readonly IKernelMemory _memory;

    public ParallelHandlersTest(
        IConfiguration cfg,
        ITestOutputHelper output) : base(cfg, output)
    {
        this._memory = new KernelMemoryBuilder()
            .WithOpenAI(this.OpenAiConfig)
            // Store data in memory
            .WithSimpleFileStorage(SimpleFileStorageConfig.Persistent)
            .WithSimpleVectorDb(SimpleVectorDbConfig.Persistent)
            .Build<MemoryServerless>();
    }

    [Fact]
    [Trait("Category", "Serverless")]
    public async Task ItUsesParallelEmbeddingGeneration()
    {
        // Arrange
        const string Id = "ItUploadsPDFDocsAndDeletes-file2-largePDF.pdf";
        Console.WriteLine("Uploading document");
        var clock = new Stopwatch();
        clock.Start();
        await this._memory.ImportDocumentAsync(
            "file2-largePDF.pdf",
            documentId: Id,
            steps: new[]
            {
                Constants.PipelineStepsExtract,
                Constants.PipelineStepsPartition,
                "gen_embeddings_parallel", // alternative to default "gen_embeddings", 3 secs vs 12 secs
                Constants.PipelineStepsSaveRecords
            });

        var count = 0;
        while (!await this._memory.IsDocumentReadyAsync(documentId: Id))
        {
            Assert.True(count++ <= 30, "Document import timed out");
            Console.WriteLine("Waiting for memory ingestion to complete...");
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        clock.Stop();

        // Act
        Console.WriteLine($"Time taken: {clock.ElapsedMilliseconds} msecs");
        var answer = await this._memory.AskAsync("What's the purpose of the planning system?");
        Console.WriteLine(answer.Result);

        // Assert
        Assert.Contains("sustainable development", answer.Result, StringComparison.OrdinalIgnoreCase);

        // Cleanup
        Console.WriteLine("Deleting memories extracted from the document");
        await this._memory.DeleteDocumentAsync(Id);
    }

    [Fact]
    [Trait("Category", "Serverless")]
    public async Task ItUsesParallelSummarization()
    {
        // Arrange
        const string Id = "ItUploadsPDFDocsAndDeletes-file2-largePDF.pdf";
        Console.WriteLine("Uploading document");
        var clock = new Stopwatch();
        clock.Start();
        await this._memory.ImportDocumentAsync(
            "file2-largePDF.pdf",
            documentId: Id,
            steps: new[]
            {
                Constants.PipelineStepsExtract,
                "summarize", // alternative to default "summarize", 55secs vs 50secs
                Constants.PipelineStepsGenEmbeddings,
                Constants.PipelineStepsSaveRecords,
            });

        var count = 0;
        while (!await this._memory.IsDocumentReadyAsync(documentId: Id))
        {
            Assert.True(count++ <= 230, "Document import timed out");
            Console.WriteLine("Waiting for summarization to complete...");
            await Task.Delay(TimeSpan.FromSeconds(3));
        }

        clock.Stop();

        // Act
        Console.WriteLine($"Time taken: {clock.ElapsedMilliseconds} msecs");
        var results = await this._memory.SearchSyntheticsAsync("summary", filter: MemoryFilters.ByDocument(Id));
        foreach (Citation result in results)
        {
            Console.WriteLine($"== {result.SourceName} summary ==\n{result.Partitions.First().Text}\n");
        }

        // Cleanup
        Console.WriteLine("Deleting memories extracted from the document");
        await this._memory.DeleteDocumentAsync(Id);
    }
}
