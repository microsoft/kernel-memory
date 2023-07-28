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

Importing documents into your Semantic Memory can be as simple as this:

```csharp
var memory = new MemoryPipelineClient();

await memory.ImportFileAsync("meeting-transcript.docx", new ImportFileOptions("user-1"));

await memory.ImportFilesAsync(new[] { "business-plan.docx", "manual.pdf" }, new ImportFileOptions("user-2"));
```

Asking questions

```csharp
string answer1 = await memory.AskAsync("How many people attended the meeting?", "user-1");

string answer2 = await memory.AskAsync("what's the project timeline?", "user-2");
```

The code leverages the default documents ingestion pipeline:

1. Extract text
2. Partition the text in small chunks
3. Extract embedding
4. Save embedding into a vector index

Data is also organized by users, protecting information and allowing to organize private information.

Users can also organize memories in **collections**:

```csharp
var memory = new MemoryPipelineClient();

await memory.ImportFilesAsync("business-plan.docx", new ImportFileOptions("user-2", "business"));

await memory.ImportFilesAsync("February's demo.pdf", new ImportFileOptions("user-2", "demos"));

await memory.ImportFilesAsync("July's demo.pdf", new ImportFileOptions("user-2", "demos"));
```

## Import memory using Semantic Memory Web Service

Depending on the configuration, the code above can run all the memory encoding
**locally inside your process, or remotely through an asynchronous service.**

If you're importing small files, and need only C# or Python, and can block
the process during the import, local-in-process execution can be fine.

However, if you are in one of these scenarios:

* I'd just like a web service to import data and send queries to answer
* My app is written in **TypeScript, Java, Rust, or some other language**
* I want to define **custom pipelines mixing multiple languages**
  like Python, TypeScript, etc
* I'm importing **big documents that can require minutes to process**, and
  I don't want to block the user interface
* I need memory import to **run independently, supporting failures and retry
  logic**

then you can deploy Semantic Memory as a service, plugging in the
default handlers or your custom Python/TypeScript/Java/etc. handlers,
and leveraging the asynchronous non-blocking memory encoding process.
[Here](server/combinedservices-dotnet/README.md) you can find a complete set
of instruction about
[how to run the Semantic Memory service](server/combinedservices-dotnet/README.md).

If you want to give the service a quick test, use the following commands.

```bash
cd server/combinedservices-dotnet

# First time configuration, creates appsettings.Development.json
# You can skip this step if you have already configured the service.
dotnet run setup
 
# Run the service with settings from appsettings.Development.json
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

To import files using Semantic Memory **web service**, use `SemanticMemoryWebClient`:

```csharp
#reference clients/MemoryWebClient.csproj

var memory = new MemoryWebClient("http://127.0.0.1:9001"); // <== URL where the web service is running

await memory.ImportFileAsync("file1.docx",
    new ImportFileOptions("user-id-1", "memory-collection"));

await memory.ImportFilesAsync(new[] { "file2.docx", "file3.pdf" },
    new ImportFileOptions("user-id-1", "memory-collection"));
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

