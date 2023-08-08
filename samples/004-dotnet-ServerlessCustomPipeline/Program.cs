// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.Client.Models;
using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.ContentStorage;
using Microsoft.SemanticMemory.Core.Handlers;
using Microsoft.SemanticMemory.Core.Pipeline;
using Microsoft.SemanticMemory.InteractiveSetup;

// Run `dotnet run setup` to run this code and setup the example
if (new[] { "setup", "-setup" }.Contains(args.FirstOrDefault(), StringComparer.OrdinalIgnoreCase))
{
    Main.InteractiveSetup(cfgService: false, cfgOrchestration: false);
}

/* Define a custom pipeline, 100% C# handlers, and run it in this process.
 * Note: no web service required to run this.
 * The pipeline might use settings in appsettings.json, but uses
 * 'InProcessPipelineOrchestrator' explicitly. */

Console.WriteLine("=== In process file import example ===");
var app = Builder.CreateBuilder(out SemanticMemoryConfig config).Build();

// Azure Blobs or FileSystem, depending on settings in appsettings.json
var storage = app.Services.GetService<IContentStorage>();

// Data pipelines orchestrator
InProcessPipelineOrchestrator orchestrator = new(storage!, app.Services);

// Add pipeline handlers
Console.WriteLine("* Defining pipeline handlers...");

TextExtractionHandler textExtraction = new("extract", orchestrator);
await orchestrator.AddHandlerAsync(textExtraction);

TextPartitioningHandler textPartitioning = new("partition", orchestrator);
await orchestrator.AddHandlerAsync(textPartitioning);

GenerateEmbeddingsHandler textEmbedding = new("gen_embeddings", orchestrator);
await orchestrator.AddHandlerAsync(textEmbedding);

SaveEmbeddingsHandler saveEmbedding = new("save_embeddings", orchestrator);
await orchestrator.AddHandlerAsync(saveEmbedding);

// orchestrator.AddHandlerAsync(...);
// orchestrator.AddHandlerAsync(...);

// Create sample pipeline with 4 files
Console.WriteLine("* Defining pipeline with 4 files...");
var pipeline = orchestrator
    .PrepareNewFileUploadPipeline("userZ", "inProcessTest", new TagCollection { { "testName", "example3" } })
    .AddUploadFile("file1", "file1-Wikipedia-Carbon.txt", "file1-Wikipedia-Carbon.txt")
    .AddUploadFile("file2", "file2-Wikipedia-Moon.txt", "file2-Wikipedia-Moon.txt")
    .AddUploadFile("file3", "file3-lorem-ipsum.docx", "file3-lorem-ipsum.docx")
    .AddUploadFile("file4", "file4-SK-Readme.pdf", "file4-SK-Readme.pdf")
    .AddUploadFile("file5", "file5-NASA-news.pdf", "file5-NASA-news.pdf")
    .Then("extract")
    .Then("partition")
    .Then("gen_embeddings")
    .Then("save_embeddings")
    .Build();

// Execute pipeline
Console.WriteLine("* Executing pipeline...");
await orchestrator.RunPipelineAsync(pipeline);

Console.WriteLine("* File import completed.");

Console.WriteLine("Refactoring in progress");
