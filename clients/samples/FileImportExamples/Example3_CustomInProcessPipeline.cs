// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.ContentStorage;
using Microsoft.SemanticMemory.Core.Handlers;
using Microsoft.SemanticMemory.Core.Pipeline;

public static class Example3_CustomInProcessPipeline
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== In process file import example ===");
        IHost app = AppBuilder.Build();

        // Azure Blobs or FileSystem, depending on settings in appsettings.json
        var storage = app.Services.GetService<IContentStorage>();

        // Data pipelines orchestrator
        InProcessPipelineOrchestrator orchestrator = new(storage!);

        // Add pipeline handlers
        Console.WriteLine("* Defining pipeline handlers...");

        TextExtractionHandler textExtraction = new("extract", orchestrator);
        await orchestrator.AddHandlerAsync(textExtraction);

        TextPartitioningHandler textPartitioning = new("partition", orchestrator);
        await orchestrator.AddHandlerAsync(textPartitioning);

        GenerateEmbeddingsHandler textEmbedding = new("gen_embeddings", orchestrator, app.Services.GetService<SemanticMemoryConfig>()!);
        await orchestrator.AddHandlerAsync(textEmbedding);

        SaveEmbeddingsHandler saveEmbedding = new("save_embeddings", orchestrator, app.Services.GetService<SemanticMemoryConfig>()!);
        await orchestrator.AddHandlerAsync(saveEmbedding);

        // orchestrator.AttachHandlerAsync(...);
        // orchestrator.AttachHandlerAsync(...);

        // Create sample pipeline with 4 files
        Console.WriteLine("* Defining pipeline with 4 files...");
        var pipeline = orchestrator
            .PrepareNewFileUploadPipeline("inProcessTest", "userId", new[] { "collection1" })
            .AddUploadFile("file1", "file1.txt", "file1.txt")
            .AddUploadFile("file2", "file2.txt", "file2.txt")
            .AddUploadFile("file3", "file3.docx", "file3.docx")
            .AddUploadFile("file4", "file4.pdf", "file4.pdf")
            .Then("extract")
            .Then("partition")
            .Then("gen_embeddings")
            .Then("save_embeddings")
            .Build();

        // Execute pipeline
        Console.WriteLine("* Executing pipeline...");
        await orchestrator.RunPipelineAsync(pipeline);

        Console.WriteLine("* File import completed.");
    }
}
