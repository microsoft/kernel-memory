# Semantic Memory

**Semantic Memory** is an open-source library and service specializing in the
efficient indexing of datasets through custom continuous data pipelines.

![image](https://github.com/microsoft/semantic-memory/assets/371009/31894afa-d19e-4e9b-8d0f-cb889bf5c77f)

Utilizing advanced embeddings and LLMs, the system enables natural language
querying for obtaining answers from the indexed data, complete with citations
and links to the original sources.

![image](https://github.com/microsoft/semantic-memory/assets/371009/c5f0f6c3-814f-45bf-b055-063f23ed80ea)

Designed for seamless integration with
[Semantic Kernel](https://github.com/microsoft/semantic-kernel),
Semantic Memory enhances data-driven features in applications built using SK.

> ℹ️ **NOTE**: the documentation below is work in progress, will evolve quickly
> as is not fully functional yet.

# Examples

## Importing memory, locally, without deployments

Importing files into your Semantic Memory can be as simple as this:

```csharp
var memory = new SemanticMemoryClient();

await memory.ImportFileAsync("file1.docx", new ImportFileOptions("user-id-1", "memory-collection"));

await memory.ImportFilesAsync(new[] { "file2.docx", "file3.pdf" }, new ImportFileOptions("user-id-1", "memory-collection"));
```

The code leverages the default data ingestion pipeline:

1. Extract text
2. Partition the text in small chunks
3. Extract embedding
4. Save embedding into a vector index

## Import memory using Semantic Memory Web Service

Depending on the configuration, the code above can run locally, inside your
process, or remotely through a service.

If you're importing small files, and need only C# or Python, and can block
the process during the import, local execution can be fine.

However, if you are in one of these scenarios:

* I'd just like a web service to import data and send queries to answer
* My app is written in **TypeScript, Java, Rust, or some other language**
* I want to define **custom pipelines mixing multiple languages**
  like Python, TypeScript, etc
* I'm importing **big documents that can require minutes to process**, and
  I don't want to block the user interface
* I need memory import to **run independently, supporting failures and retry
  logic**

then you can deploy Semantic Memory as a web service, plugging in the
default handlers or your custom Python/TypeScript/Java/etc. handlers,
leveraging the asynchronous queues automatically available.

If you deploy the **default web service available in the repo**, you only
need to change the configuration, and use the same code above.

To import files using Semantic Memory web service, simply use `SemanticMemoryWebClient`:

```csharp
var memory = new SemanticMemoryWebClient("http://127.0.0.1:9001"); // <== Web Client

await memory.ImportFileAsync("file1.docx", new ImportFileOptions("user-id-1", "memory-collection"));

await memory.ImportFilesAsync(new[] { "file2.docx", "file3.pdf" }, new ImportFileOptions("user-id-1", "memory-collection"));
```


## Custom import pipelines

On the other hand, if you need a custom data pipeline, you can also
customize the steps, which will be handled by your custom business logic:

```csharp
var app = AppBuilder.Build();
var storage = app.Services.GetService<IContentStorage>();

// Use a local, synchronous, orchestrator
var orchestrator = new InProcessPipelineOrchestrator(storage);

// Define custom .NET handlers
var step1 = new MyHandler1("step1", orchestrator);
var step2 = new MyHandler2("step2", orchestrator);
var step3 = new MyHandler3("step3", orchestrator);
await orchestrator.AddHandlerAsync(step1);
await orchestrator.AddHandlerAsync(step2);
await orchestrator.AddHandlerAsync(step3);

// Instantiate a custom pipeline
var pipeline = orchestrator
    .PrepareNewFileUploadPipeline("mytest", "user-id-1", new[] { "memory-collection" })
    .AddUploadFile("file1", "file1.docx", "file1.docx")
    .AddUploadFile("file2", "file2.pdf", "file2.pdf")
    .Then("step1")
    .Then("step2")
    .Then("step3")
    .Build();

// Execute in process, process all files with all the handlers
await orchestrator.RunPipelineAsync(pipeline);
```

