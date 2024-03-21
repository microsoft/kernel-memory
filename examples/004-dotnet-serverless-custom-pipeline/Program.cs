// Copyright (c) Microsoft. All rights reserved.

/* This example shows how to setup KM pipeline with custom handlers, to process files with custom logic.
 * In this example handlers are executed synchronously when calling "ImportDocumentAsync".
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
 *  - If you are not using queues (MemoryServerless class), see the example below, custom handlers are added via the ORCHESTRATOR
 *  - If you are using queues (MemoryService class) see example 006
 *  - If you are using the Memory Web Service
 *     - the list of handlers can be configured in appsettings.json and appsettings.<ENV>.json
 *     - You can create new handlers and load them in the configuration, passing the path to the assembly and the handler class.
 */

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Handlers;

var memoryBuilder = new KernelMemoryBuilder()
    .WithoutDefaultHandlers() // remove default handlers, added manually below
    .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"));

var memory = memoryBuilder.Build<MemoryServerless>();

/*********************************************************
 * Define custom handlers
 *********************************************************/

Console.WriteLine("* Registering pipeline handlers...");

memory.Orchestrator.AddHandler<TextExtractionHandler>("extract_text");
memory.Orchestrator.AddHandler<TextPartitioningHandler>("split_text_in_partitions");
memory.Orchestrator.AddHandler<GenerateEmbeddingsHandler>("generate_embeddings");
memory.Orchestrator.AddHandler<SummarizationHandler>("summarize");
memory.Orchestrator.AddHandler<SaveRecordsHandler>("save_memory_records");

/*********************************************************
 * Import files using custom handlers
 *********************************************************/

// Use the custom handlers with the memory object
await memory.ImportDocumentAsync(
    new Document("inProcessTest")
        .AddFile("file1-Wikipedia-Carbon.txt")
        .AddFile("file2-Wikipedia-Moon.txt")
        .AddFile("file3-lorem-ipsum.docx")
        .AddFile("file4-KM-Readme.pdf")
        .AddFile("file5-NASA-news.pdf")
        .AddTag("testName", "example3"),
    index: "user-id-1",
    steps: new[]
    {
        "extract_text",
        "split_text_in_partitions",
        "generate_embeddings",
        "save_memory_records"
    });

Console.WriteLine("* File import completed.");
