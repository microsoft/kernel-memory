// Copyright (c) Microsoft. All rights reserved.

/* This example shows how to setup KM pipeline with custom handlers and queues, to process files with custom logic and durable queues.
 * In this example handlers are executed asynchronously, using queues, when calling "ImportDocumentAsync".
 *
 * The ingestion pipeline is composed by a set of default STEPS to process documents:
 *  - extract
 *  - partition
 *  - gen_embeddings
 *  - save_records
 *
 * Each step is managed by a HANDLER, see the Core/Handlers for a list of available handlers.
 *
 * You can create new handlers, and customize the pipeline in multiple ways:
 *
 *  - Call .WithoutDefaultHandlers() to remove the default handlers
 *  - If you are using queues (MemoryService class), see the example below, custom handlers are added as SERVICES in the HOSTING APPLICATION
 *  - If you are not using queues (MemoryServerless class), see example 004
 *  - If you are using the Memory Web Service
 *     - the list of handlers can be configured in appsettings.json and appsettings.<ENV>.json
 *     - You can create new handlers and load them in the configuration, passing the path to the assembly and the handler class.
 */

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Handlers;

// Alternative for web apps: var host = WebApplication.CreateBuilder();
var host = new HostApplicationBuilder();

var memoryBuilder = new KernelMemoryBuilder(host.Services)
    .WithoutDefaultHandlers() // remove default handlers, add our custom ones below
    .WithSimpleQueuesPipeline()
    .WithOpenAIDefaults(Environment.GetEnvironmentVariable("OPENAI_API_KEY")!);

/*********************************************************
 * Define custom handlers
 *********************************************************/

Console.WriteLine("* Registering custom pipeline handlers...");

host.Services.AddHandlerAsHostedService<TextExtractionHandler>("extract_text");
host.Services.AddHandlerAsHostedService<TextPartitioningHandler>("split_text_in_partitions");
host.Services.AddHandlerAsHostedService<SummarizationHandler>("summarize");
host.Services.AddHandlerAsHostedService<GenerateEmbeddingsHandler>("generate_embeddings");
host.Services.AddHandlerAsHostedService<SaveRecordsHandler>("save_memory_records");

/*********************************************************
 * Start asynchronous handlers
 *********************************************************/

// Notes:
// * It's recommended building the Memory before building the hosting app, because KM builder
//   might register missing dependencies in the shared service collection.
// * Build() and Build<MemoryService>() in this case are equivalent because we added a queue service
var memory = memoryBuilder.Build<MemoryService>();

var hostingApp = host.Build();

#pragma warning disable CS4014 // Run handlers in the background
hostingApp.RunAsync();
#pragma warning restore CS4014

/*********************************************************
 * Import files using custom handlers
 *********************************************************/

// Use the custom handlers with the memory object
string docId = await memory.ImportDocumentAsync(
    new Document("inProcessTest")
        .AddFile("file1-Wikipedia-Carbon.txt")
        .AddFile("file2-Wikipedia-Moon.txt")
        .AddFile("file3-lorem-ipsum.docx")
        .AddFile("file4-KM-Readme.pdf")
        .AddFile("file5-NASA-news.pdf")
        .AddTag("testName", "example3"),
    steps:
    [
        "extract_text",
        "split_text_in_partitions",
        "generate_embeddings",
        "save_memory_records"
    ]);

Console.WriteLine("* File import started.");

// Wait for import to complete
var status = await memory.GetDocumentStatusAsync(documentId: docId);
while (status is { Completed: false })
{
    Console.WriteLine("* Work in progress...");
    Console.WriteLine("Steps:     " + string.Join(", ", status.Steps));
    Console.WriteLine("Completed: " + string.Join(", ", status.CompletedSteps));
    Console.WriteLine("Remaining: " + string.Join(", ", status.RemainingSteps));
    Console.WriteLine();
    await Task.Delay(TimeSpan.FromSeconds(1));
    status = await memory.GetDocumentStatusAsync(documentId: docId);
}

Console.WriteLine("* File import completed.");

/*********************************************************
 * Stop asynchronous handlers
 *********************************************************/

await hostingApp.StopAsync();
