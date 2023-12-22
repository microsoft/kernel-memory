// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Handlers;

var memoryBuilder = new KernelMemoryBuilder()
    // .WithAzureAISearch(Env.Var("AZSEARCH_ENDPOINT"), Env.Var("AZSEARCH_API_KEY")) => To use Azure AI Search
    // .WithQdrant("http://127.0.0.1:6333") => To use Qdrant docker
    .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"));

var _ = memoryBuilder.Build();
var orchestrator = memoryBuilder.GetOrchestrator();

// Add pipeline handlers
Console.WriteLine("* Defining pipeline handlers...");

TextExtractionHandler textExtraction = new("extract", orchestrator);
await orchestrator.AddHandlerAsync(textExtraction);

TextPartitioningHandler textPartitioning = new("partition", orchestrator);
await orchestrator.AddHandlerAsync(textPartitioning);

SummarizationHandler summarizeEmbedding = new("summarize", orchestrator);
await orchestrator.AddHandlerAsync(summarizeEmbedding);

GenerateEmbeddingsHandler textEmbedding = new("gen_embeddings", orchestrator);
await orchestrator.AddHandlerAsync(textEmbedding);

SaveRecordsHandler saveRecords = new("save_records", orchestrator);
await orchestrator.AddHandlerAsync(saveRecords);

// orchestrator.AddHandlerAsync(...);
// orchestrator.AddHandlerAsync(...);

// Create sample pipeline with 4 files
Console.WriteLine("* Defining pipeline with 4 files...");
var pipeline = orchestrator
    .PrepareNewDocumentUpload(index: "tests", documentId: "inProcessTest", new TagCollection { { "testName", "example3" } })
    .AddUploadFile("file1", "file1-Wikipedia-Carbon.txt", "file1-Wikipedia-Carbon.txt")
    .AddUploadFile("file2", "file2-Wikipedia-Moon.txt", "file2-Wikipedia-Moon.txt")
    .AddUploadFile("file3", "file3-lorem-ipsum.docx", "file3-lorem-ipsum.docx")
    .AddUploadFile("file4", "file4-SK-Readme.pdf", "file4-SK-Readme.pdf")
    .AddUploadFile("file5", "file5-NASA-news.pdf", "file5-NASA-news.pdf")
    .Then("extract")
    .Then("partition")
    .Then("summarize")
    .Then("gen_embeddings")
    .Then("save_records")
    .Build();

// Execute pipeline
Console.WriteLine("* Executing pipeline...");
await orchestrator.RunPipelineAsync(pipeline);

Console.WriteLine("* File import completed.");

Console.WriteLine("Refactoring in progress");
