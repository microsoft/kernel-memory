# Semantic Memory

**Semantic Memory** is an open-source library and [service](dotnet/Service)
specialized in the efficient indexing of datasets through custom continuous data
pipelines.

![image](https://github.com/microsoft/semantic-memory/assets/371009/31894afa-d19e-4e9b-8d0f-cb889bf5c77f)

Utilizing advanced embeddings and LLMs, the system enables Natural Language
querying for obtaining answers from the indexed data, complete with citations
and links to the original sources.

![image](https://github.com/microsoft/semantic-memory/assets/371009/c5f0f6c3-814f-45bf-b055-063f23ed80ea)

Designed for seamless integration with
[Semantic Kernel](https://github.com/microsoft/semantic-kernel),
Semantic Memory enhances data-driven features in applications built using SK.

> ℹ️ **NOTE**: the documentation below is work in progress, will evolve quickly
> as is not fully functional yet.

# Semantic Memory in serverless mode

Semantic Memory works and scales at best when running as a service, allowing to
ingest thousands of documents and information without blocking your app.

However, you can use Semantic Memory also serverless, embedding the `MemoryPipelineClient`
in your app.

> ### Importing documents into your Semantic Memory can be as simple as this:
>
> ```csharp
> var memory = new MemoryPipelineClient();
>
> // Import a file (default user)
> await memory.ImportFileAsync("meeting-transcript.docx");
>
> // Import a file specifying a User and Tags
> await memory.ImportFileAsync("business-plan.docx",
>     new DocumentDetails("file1", "user@some.email")
>         .AddTag("collection", "business")
>         .AddTag("collection", "plans")
>         .AddTag("type", "doc"));
> ```

> ### Asking questions:
>
> ```csharp
> string answer1 = await memory.AskAsync("How many people attended the meeting?");
>
> string answer2 = await memory.AskAsync("what's the project timeline?", "user@some.email");
> ```

The code leverages the default documents ingestion pipeline:

1. Extract text: recognize the file format and extract the information
2. Partition the text in small chunks, to optimize search
3. Extract embedding using an LLM embedding generator
4. Save embedding into a vector index such as
   [Azure Cognitive Search](https://learn.microsoft.com/en-us/azure/search/vector-search-overview),
   [Qdrant](https://qdrant.tech/) or other DBs.

Documents are organized by users, safeguarding their private information.
Furthermore, memories can be categorized and structured using **tags**, enabling
efficient search and retrieval through faceted navigation.

## Using Semantic Memory Service

Depending on your scenarios, you might want to run all the code **locally
inside your process, or remotely through an asynchronous service.**

If you're importing small files, and need only C# or only Python, and can block
the process during the import, local-in-process execution can be fine, using
the **MemoryPipelineClient** seen above.

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
and leveraging the asynchronous non-blocking memory encoding process,
sending documents and asking questions using the **MemoryWebClient**.

[Here](dotnet/Service/README.md) you can find a complete set of instruction
about [how to run the Semantic Memory service](dotnet/Service/README.md).

If you want to give the service a quick test, use the following command
to **start the Semantic Memory Service**:

> ### On WSL / Linux / MacOS:
>
> ```shell
> cd dotnet/Service
> ./setup.sh
> ./run.sh
> ```

> ### On Windows:
>
> ```shell
> cd dotnet/Service
> setup.cmd
> run.cmd
> ```

> ### To import files using Semantic Memory **web service**, use `MemoryWebClient`:
>
> ```csharp
> #reference dotnet/ClientLib/ClientLib.csproj
>
> var memory = new MemoryWebClient("http://127.0.0.1:9001"); // <== URL where the web service is running
>
> await memory.ImportFileAsync("meeting-transcript.docx");
>
> await memory.ImportFileAsync("business-plan.docx",
>     new DocumentDetails("file1", "user0022")
>         .AddTag("collection", "business")
>         .AddTag("collection", "plans")
>         .AddTag("type", "doc"));
> ```

You can find a [full example here](samples/dotnet-WebClient/).

## Custom memory ingestion pipelines

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

# Examples and Tools

1. [Using the web service](samples/dotnet-WebClient)
2. [Importing files without the service (serverless ingestion)](samples/dotnet-Serverless)
3. [How to upload files from command line with curl](samples/curl)
4. [Writing a custom pipeline handler](samples/dotnet-CustomHandler)
5. [Importing files with custom steps](samples/dotnet-ServerlessCustomPipeline)
6. [Extracting text from documents](samples/dotnet-ExtractTextFromDocs)
7. [Curl script to upload files](tools/upload-file.sh)
8. [Script to start RabbitMQ for development tasks](tools/run-rabbitmq.sh)
