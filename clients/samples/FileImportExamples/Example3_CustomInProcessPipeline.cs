// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel.SemanticMemory.Core.Configuration;
using Microsoft.SemanticKernel.SemanticMemory.Core.ContentStorage;
using Microsoft.SemanticKernel.SemanticMemory.Core.Handlers;
using Microsoft.SemanticKernel.SemanticMemory.Core.Pipeline;

public static class Example3_CustomInProcessPipeline
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== In process file import example ===");
        IHost app = AppBuilder.Build();

        // Azure Blobs or FileSystem, depending on settings in appsettings.json
        var storage = app.Services.GetService<IContentStorage>();

        // Data pipelines orchestrator
        var orchestrator = new InProcessPipelineOrchestrator(storage!);

        // Add pipeline handlers
        Console.WriteLine("* Defining pipeline handlers...");

        var textExtraction = new TextExtractionHandler("extract", orchestrator);
        await orchestrator.AddHandlerAsync(textExtraction);

        var textPartitioning = new TextPartitioningHandler("partition", orchestrator);
        await orchestrator.AddHandlerAsync(textPartitioning);

        // orchestrator.AttachHandlerAsync(...);
        // orchestrator.AttachHandlerAsync(...);

        // Create sample pipeline with 4 files
        Console.WriteLine("* Defining pipeline with 4 files...");
        var pipeline = orchestrator
            .PrepareNewFileUploadPipeline("inProcessTest", "userId", new[] { "vault1" })
            .AddUploadFile("file1", "file1.txt", "file1.txt")
            .AddUploadFile("file2", "file2.txt", "file2.txt")
            .AddUploadFile("file3", "file3.docx", "file3.docx")
            .AddUploadFile("file4", "file4.pdf", "file4.pdf")
            .Then("extract")
            .Then("partition")
            // .Then("index")
            .Build();

        // Execute pipeline
        Console.WriteLine("* Executing pipeline...");
        await orchestrator.RunPipelineAsync(pipeline);

        Console.WriteLine("* File import completed.");
    }
}
