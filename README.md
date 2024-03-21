# Kernel Memory

[![License: MIT](https://img.shields.io/github/license/microsoft/kernel-memory)](https://github.com/microsoft/kernel-memory/blob/main/LICENSE)
[![Discord](https://img.shields.io/discord/1063152441819942922?label=Discord&logo=discord&logoColor=white&color=d82679)](https://aka.ms/KMdiscord)

**Kernel Memory** (KM) is a **multi-modal [AI Service](service/Service/README.md)**
specialized in the efficient indexing of datasets through custom continuous data
hybrid pipelines, with support for
**[Retrieval Augmented Generation](https://en.wikipedia.org/wiki/Prompt_engineering#Retrieval-augmented_generation)** (RAG),
synthetic memory, prompt engineering, and custom semantic memory processing.

KM includes a GPT **[Plugin](https://www.microsoft.com/en-us/microsoft-365/blog/2023/05/23/empowering-every-developer-with-plugins-for-microsoft-365-copilot/)**,
**web clients**, a .NET library for embedded applications, and as a
[Docker container](https://hub.docker.com/r/kernelmemory/service).

![image](https://github.com/microsoft/kernel-memory/assets/371009/31894afa-d19e-4e9b-8d0f-cb889bf5c77f)

Utilizing advanced embeddings and LLMs, the system enables Natural Language
querying for obtaining answers from the indexed data, complete with citations
and links to the original sources.

![image](https://github.com/microsoft/kernel-memory/assets/371009/c5f0f6c3-814f-45bf-b055-063f23ed80ea)

Designed for seamless integration as a Plugin with
[Semantic Kernel](https://github.com/microsoft/semantic-kernel), Microsoft
Copilot and ChatGPT, Kernel Memory enhances data-driven features in applications
built for most popular AI platforms.

## Repository Guidance

This repository presents best practices and a reference architecture for memory in specific
AI and LLMs application scenarios. Please note that **the provided code serves as a
demonstration** and is **not an officially supported** Microsoft offering.

## Kernel Memory (KM) and Semantic Memory (SM)

**Semantic Memory (SM) is a library for C#, Python, and Java** that wraps direct calls
to databases and supports vector search. It was developed as part of the Semantic
Kernel (SK) project and serves as the first public iteration of long-term memory.
The core library is maintained in three languages, while the list of supported
storage engines (known as "connectors") varies across languages.

**Kernel Memory (KM) is a service** built on the feedback received and lessons learned
from developing Semantic Kernel (SK) and Semantic Memory (SM). It provides several
features that would otherwise have to be developed manually, such as storing files,
extracting text from files, providing a framework to secure users' data, etc.
The KM codebase is entirely in .NET, which eliminates the need to write and maintain
features in multiple languages. As a service, **KM can be used from any language, tool,
or platform, e.g. browser extensions and ChatGPT assistants.**

Here's a few notable differences:

| Feature          | Semantic Memory                                                                                              | Kernel Memory                                                                                                                                                          |
|------------------|--------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Data formats     | Text only                                                                                                    | Web pages, PDF, Images, Word, PowerPoint, Excel, Markdown, Text, JSON, more being added                                                                                |
| Search           | Cosine similarity                                                                                            | Cosine similarity, Hybrid search with filters, AND/OR conditions                                                                                                       |
| Language support | C#, Python, Java                                                                                             | Any language, command line tools, browser extensions, low-code/no-code apps, chatbots, assistants, etc.                                                                |
| Storage engines  | Azure AI Search, Chroma, DuckDB, Kusto, Milvus, MongoDB, Pinecone, Postgres, Qdrant, Redis, SQLite, Weaviate | Azure AI Search, Elasticsearch, MongoDB Atlas, Postgres, Qdrant, Redis, SQL Server, In memory KNN, On disk KNN. In progress: Azure Cosmos DB for MongoDB vCore, Chroma |

and **features available only in Kernel Memory**:

* RAG (Retrieval Augmented Generation)
* RAG sources lookup
* Summarization
* Security Filters (filter memory by users and groups)
* Long running ingestion, large documents, with retry logic and durable queues
* Custom tokenization
* Document storage
* OCR via Azure Document Intelligence
* LLMs (Large Language Models) with dedicated tokenization
* Cloud deployment
* OpenAPI
* Custom storage schema (partially implemented/work in progress)
* Short Term Memory (partially implemented/work in progress)
* Concurrent write to multiple vector DBs

# Supported Data formats and Backends

* 📝 MS Office: Word, Excel, PowerPoint
* 📃 PDF documents
* 🌐 Fetch web pages and HTML files
* 🖼️ JPG/PNG/TIFF Images with text via OCR
* 📄 MarkDown and Raw plain text
* 💻 JSON files
* 💡 AI:
  [Azure OpenAI](https://learn.microsoft.com/azure/ai-services/openai/concepts/models),
  [OpenAI](https://platform.openai.com/docs/models),
  LLama - thanks to [llama.cpp](https://github.com/ggerganov/llama.cpp) and [LLamaSharp](https://github.com/SciSharp/LLamaSharp),
  [LM Studio](https://lmstudio.ai/),
  [Azure Document Intelligence](https://azure.microsoft.com/products/ai-services/ai-document-intelligence)
* 🧠 Vector storage:
  [Azure AI Search](https://azure.microsoft.com/products/ai-services/ai-search),
  [Postgres+pgvector](https://github.com/microsoft/kernel-memory/extensions/postgres),
  [Qdrant](https://qdrant.tech),
  [MSSQL Server (third party)](https://www.nuget.org/packages/KernelMemory.MemoryStorage.SqlServer),
  [Elasticsearch (third party)](https://www.nuget.org/packages/FreeMindLabs.KernelMemory.Elasticsearch),
  [Redis](https://redis.io),
  [Chroma (work in progress)](https://www.trychroma.com),
  KNN vectors in memory (volatile),
  KNN vectors on disk (persistent).
* 🗂 Content storage: [Azure Blobs](https://learn.microsoft.com/azure/storage/blobs/storage-blobs-introduction),
  Local file system,
  In memory content (volatile).
* ⏳ Orchestration: [Azure Queues](https://learn.microsoft.com/azure/storage/queues/storage-queues-introduction),
  [RabbitMQ](https://www.rabbitmq.com),
  Local file based queues,
  In memory queues (volatile).

# Kernel Memory in serverless mode

Kernel Memory works and scales at best when running as a service, allowing to
ingest thousands of documents and information without blocking your app.

However, you can use Kernel Memory also serverless, embedding the `MemoryServerless`
class in your app.

> ### Importing documents into your Kernel Memory can be as simple as this:
>
> ```csharp
> var memory = new KernelMemoryBuilder()
>     .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"))
>     .Build<MemoryServerless>();
>
> // Import a file
> await memory.ImportDocumentAsync("meeting-transcript.docx", tags: new() { { "user", "Blake" } });
>
> // Import multiple files and apply multiple tags
> await memory.ImportDocumentAsync(new Document("file001")
>     .AddFile("business-plan.docx")
>     .AddFile("project-timeline.pdf")
>     .AddTag("user", "Blake")
>     .AddTag("collection", "business")
>     .AddTag("collection", "plans")
>     .AddTag("fiscalYear", "2023"));
> ```

> ### Asking questions:
>
> ```csharp
> var answer1 = await memory.AskAsync("How many people attended the meeting?");
>
> var answer2 = await memory.AskAsync("what's the project timeline?", filter: new MemoryFilter().ByTag("user", "Blake"));
> ```

The code leverages the default documents ingestion pipeline:

1. Extract text: recognize the file format and extract the information
2. Partition the text in small chunks, to optimize search
3. Extract embedding using an LLM embedding generator
4. Save embedding into a vector index such as
   [Azure AI Search](https://learn.microsoft.com/azure/search/vector-search-overview),
   [Qdrant](https://qdrant.tech/) or other DBs.

Documents are organized by users, safeguarding their private information.
Furthermore, memories can be categorized and structured using **tags**, enabling
efficient search and retrieval through faceted navigation.

# Data lineage, citations

All memories and answers are fully correlated to the data provided. When
producing an answer, Kernel Memory includes all the information needed
to verify its accuracy:

```csharp
await memory.ImportFileAsync("NASA-news.pdf");

var answer = await memory.AskAsync("Any news from NASA about Orion?");

Console.WriteLine(answer.Result + "/n");

foreach (var x in answer.RelevantSources)
{
    Console.WriteLine($"  * {x.SourceName} -- {x.Partitions.First().LastUpdate:D}");
}
```

> Yes, there is news from NASA about the Orion spacecraft. NASA has invited the
> media to see a new test version of the Orion spacecraft and the hardware that
> will be used to recover the capsule and astronauts upon their return from
> space during the Artemis II mission. The event is scheduled to take place at
> Naval Base San Diego on Wednesday, August 2, at 11 a.m. PDT. Personnel from
> NASA, the U.S. Navy, and the U.S. Air Force will be available to speak with
> the media. Teams are currently conducting tests in the Pacific Ocean to
> demonstrate and evaluate the processes, procedures, and hardware for recovery
> operations for crewed Artemis missions. These tests will help prepare the
> team for Artemis II, which will be NASA's first crewed mission under the
> Artemis program. The Artemis II crew, consisting of NASA astronauts Reid
> Wiseman, Victor Glover, and Christina Koch, and Canadian Space Agency
> astronaut Jeremy Hansen, will participate in recovery testing at sea next
> year. For more information about the Artemis program, you can visit the NASA
> website.
>
> - **NASA-news.pdf -- Tuesday, August 1, 2023**

## Using Kernel Memory Service

Depending on your scenarios, you might want to run all the code **locally
inside your process, or remotely through an asynchronous service.**

If you're importing small files, and need only C# and can block
the process during the import, local-in-process execution can be fine, using
the **MemoryServerless** seen above.

However, if you are in one of these scenarios:

* I'd just like a web service to import data and send queries to answer
* My app is written in **TypeScript, Java, Rust, or some other language**
* I want to define **custom pipelines mixing multiple languages**
  like Python, TypeScript, etc
* I'm importing **big documents that can require minutes to process**, and
  I don't want to block the user interface
* I need memory import to **run independently, supporting failures and retry
  logic**

then you can deploy Kernel Memory as a service, plugging in the
default handlers, or your custom Python/TypeScript/Java/etc. handlers,
and leveraging the asynchronous non-blocking memory encoding process,
sending documents and asking questions using the **MemoryWebClient**.

[Here](service/Service/README.md) you can find a complete set of instruction
about [how to run the Kernel Memory service](service/Service/README.md).

## Quick test using the Docker image
 
If you want to give the service a quick test, use the following command
to **start the Kernel Memory Service** using OpenAI:

```shell
docker run -e OPENAI_API_KEY="..." -it --rm -p 9001:9001 kernelmemory/service
```

If you prefer using custom settings and services such as Azure OpenAI, Azure
Document Intelligence, etc., you should create an `appsettings.Development.json`
file overriding the default values set in `appsettings.json`, or using the
configuration wizard included:

    cd service/Service
    dotnet run setup

Then run this command to start the [Docker image](https://hub.docker.com/r/kernelmemory/service)
with the configuration just created:

on Windows:

    docker run --volume .\appsettings.Development.json:/app/appsettings.Production.json -it --rm -p 9001:9001 kernelmemory/service

on macOS/Linux:

    docker run --volume ./appsettings.Development.json:/app/appsettings.Production.json -it --rm -p 9001:9001 kernelmemory/service

### To import files using Kernel Memory **web service**, use `MemoryWebClient`:

> ```csharp
> #reference clients/WebClient/WebClient.csproj
>
> var memory = new MemoryWebClient("http://127.0.0.1:9001"); // <== URL where the web service is running
>
> // Import a file (default user)
> await memory.ImportDocumentAsync("meeting-transcript.docx");
>
> // Import a file specifying a Document ID, User and Tags
> await memory.ImportDocumentAsync("business-plan.docx",
>     new DocumentDetails("user@some.email", "file001")
>         .AddTag("collection", "business")
>         .AddTag("collection", "plans")
>         .AddTag("fiscalYear", "2023"));
> ```

### Getting answers via the web service

> ```
> curl http://127.0.0.1:9001/ask -d'{"query":"Any news from NASA about Orion?"}' -H 'Content-Type: application/json'
> ```
> ```json
> {
>   "Query": "Any news from NASA about Orion?",
>   "Text": "Yes, there is news from NASA about the Orion spacecraft. NASA has invited the media to see a new test version of the Orion spacecraft and the hardware that will be used to recover the capsule and astronauts upon their return from space during the Artemis II mission. The event is scheduled to take place at Naval Base San Diego on August 2nd at 11 a.m. PDT. Personnel from NASA, the U.S. Navy, and the U.S. Air Force will be available to speak with the media. Teams are currently conducting tests in the Pacific Ocean to demonstrate and evaluate the processes, procedures, and hardware for recovery operations for crewed Artemis missions. These tests will help prepare the team for Artemis II, which will be NASA's first crewed mission under the Artemis program. The Artemis II crew, consisting of NASA astronauts Reid Wiseman, Victor Glover, and Christina Koch, and Canadian Space Agency astronaut Jeremy Hansen, will participate in recovery testing at sea next year. For more information about the Artemis program, you can visit the NASA website.",
>   "RelevantSources": [
>     {
>       "Link": "...",
>       "SourceContentType": "application/pdf",
>       "SourceName": "file5-NASA-news.pdf",
>       "Partitions": [
>         {
>           "Text": "Skip to main content\nJul 28, 2023\nMEDIA ADVISORY M23-095\nNASA Invites Media to See Recovery Craft for\nArtemis Moon Mission\n(/sites/default/ﬁles/thumbnails/image/ksc-20230725-ph-fmx01_0003orig.jpg)\nAboard the USS John P. Murtha, NASA and Department of Defense personnel practice recovery operations for Artemis II in July. A\ncrew module test article is used to help verify the recovery team will be ready to recovery the Artemis II crew and the Orion spacecraft.\nCredits: NASA/Frank Michaux\nMedia are invited to see the new test version of NASA’s Orion spacecraft and the hardware teams will use\nto recover the capsule and astronauts upon their return from space during the Artemis II\n(http://www.nasa.gov/artemis-ii) mission. The event will take place at 11 a.m. PDT on Wednesday, Aug. 2,\nat Naval Base San Diego.\nPersonnel involved in recovery operations from NASA, the U.S. Navy, and the U.S. Air Force will be\navailable to speak with media.\nU.S. media interested in attending must RSVP by 4 p.m., Monday, July 31, to the Naval Base San Diego\nPublic Aﬀairs (mailto:nbsd.pao@us.navy.mil) or 619-556-7359.\nOrion Spacecraft (/exploration/systems/orion/index.html)\nNASA Invites Media to See Recovery Craft for Artemis Moon Miss... https://www.nasa.gov/press-release/nasa-invites-media-to-see-recov...\n1 of 3 7/28/23, 4:51 PMTeams are currently conducting the ﬁrst in a series of tests in the Paciﬁc Ocean to demonstrate and\nevaluate the processes, procedures, and hardware for recovery operations (https://www.nasa.gov\n/exploration/systems/ground/index.html) for crewed Artemis missions. The tests will help prepare the\nteam for Artemis II, NASA’s ﬁrst crewed mission under Artemis that will send four astronauts in Orion\naround the Moon to checkout systems ahead of future lunar missions.\nThe Artemis II crew – NASA astronauts Reid Wiseman, Victor Glover, and Christina Koch, and CSA\n(Canadian Space Agency) astronaut Jeremy Hansen – will participate in recovery testing at sea next year.\nFor more information about Artemis, visit:\nhttps://www.nasa.gov/artemis (https://www.nasa.gov/artemis)\n-end-\nRachel Kraft\nHeadquarters, Washington\n202-358-1100\nrachel.h.kraft@nasa.gov (mailto:rachel.h.kraft@nasa.gov)\nMadison Tuttle\nKennedy Space Center, Florida\n321-298-5868\nmadison.e.tuttle@nasa.gov (mailto:madison.e.tuttle@nasa.gov)\nLast Updated: Jul 28, 2023\nEditor: Claire O’Shea\nTags:  Artemis (/artemisprogram),Ground Systems (http://www.nasa.gov/exploration/systems/ground\n/index.html),Kennedy Space Center (/centers/kennedy/home/index.html),Moon to Mars (/topics/moon-to-\nmars/),Orion Spacecraft (/exploration/systems/orion/index.html)\nNASA Invites Media to See Recovery Craft for Artemis Moon Miss... https://www.nasa.gov/press-release/nasa-invites-media-to-see-recov...\n2 of 3 7/28/23, 4:51 PM",
>           "Relevance": 0.8430657,
>           "SizeInTokens": 863,
>           "LastUpdate": "2023-08-01T08:15:02-07:00"
>         }
>       ]
>     }
>   ]
> }
> ```

You can find a [full example here](examples/001-dotnet-WebClient/README.md).

## Custom memory ingestion pipelines

On the other hand, if you need a custom data pipeline, you can also
customize the steps, which will be handled by your custom business logic:

```csharp
// Memory setup, e.g. how to calculate and where to store embeddings
var memoryBuilder = new KernelMemoryBuilder()
    .WithoutDefaultHandlers()
    .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"));

var memory = memoryBuilder.Build();

// Plug in custom .NET handlers
memory.Orchestrator.AddHandler<MyHandler1>("step1");
memory.Orchestrator.AddHandler<MyHandler2>("step2");
memory.Orchestrator.AddHandler<MyHandler3>("step3");

// Use the custom handlers with the memory object
await memory.ImportDocumentAsync(
    new Document("mytest001")
        .AddFile("file1.docx")
        .AddFile("file2.pdf"),
    steps: new[] { "step1", "step2", "step3" });
```

# Web API specs

The API schema is available at http://127.0.0.1:9001/swagger/index.html when
running the service locally with OpenAPI enabled.

# Examples and Tools

## Examples

1. [Collection of Jupyter notebooks with various scenarios](examples/000-notebooks)
2. [Using Kernel Memory web service to upload documents and answer questions](examples/001-dotnet-WebClient)
3. [Using KM Plugin for Semantic Kernel](examples/002-dotnet-SemanticKernel-plugin)
4. [Importing files and asking question without running the service (serverless mode)](examples/003-dotnet-Serverless)
5. [Processing files with custom logic (custom handlers) in serverless mode](examples/004-dotnet-serverless-custom-pipeline)
6. [Processing files with custom logic (custom handlers) in asynchronous mode](examples/005-dotnet-AsyncMemoryCustomPipeline)
7. [Upload files and ask questions from command line using curl](examples/006-curl-calling-webservice)
8. [Customizing RAG and summarization prompts](examples/101-dotnet-custom-Prompts)
9. [Custom partitioning/text chunking options](examples/102-dotnet-custom-partitioning-options)
10. [Using a custom embedding/vector generator](examples/103-dotnet-custom-EmbeddingGenerator)
11. [Using custom LLMs](examples/104-dotnet-custom-LLM)
12. [Using LLama](examples/105-dotnet-serverless-llamasharp)
13. [Summarizing documents, using synthetic memories](examples/106-dotnet-retrieve-synthetics)
14. [Writing and using a custom ingestion handler](examples/201-dotnet-serverless-custom-handler)
15. [Running a single asynchronous pipeline handler as a standalone service](examples/202-dotnet-custom-handler-as-a-service)
16. [Test project using KM package from nuget.org](examples/203-dotnet-using-core-nuget)
17. [Integrating Memory with ASP.NET applications and controllers](examples/204-dotnet-ASP.NET-MVC-integration)
18. [Sample code showing how to extract text from files](examples/205-dotnet-extract-text-from-docs)
19. [.NET configuration and logging](examples/206-dotnet-configuration-and-logging)
20. [Expanding chunks retrieving adjacent partitions](examples/207-dotnet-expanding-chunks-on-retrieval)
21. [Using local models via LM Studio](examples/208-dotnet-lmstudio)

## Tools

1. [.NET appsettings.json generator](tools/InteractiveSetup)
2. [Curl script to upload files](tools/upload-file.sh)
3. [Curl script to ask questions](tools/ask.sh)
4. [Curl script to search documents](tools/search.sh)
5. [Script to start Qdrant for development tasks](tools/run-qdrant.sh)
6. [Script to start Elasticsearch for development tasks](tools/run-elasticsearch.sh)
7. [Script to start MS SQL Server for development tasks](tools/run-mssql.sh)
8. [Script to start Redis for development tasks](tools/run-redis.sh)
9. [Script to start RabbitMQ for development tasks](tools/run-rabbitmq.sh)
10. [Script to start MongoDB Atlas for development tasks](tools/run-mongodb-atlas.sh)

### .NET packages

* **Microsoft.KernelMemory.WebClient:** The web client library, can be used to call
  a running instance of the Memory web service. .NET Standard 2.0 compatible.

  [![Nuget package](https://img.shields.io/nuget/vpre/Microsoft.KernelMemory.WebClient)](https://www.nuget.org/packages/Microsoft.KernelMemory.WebClient/)
  [![Example code](https://img.shields.io/badge/example-code-blue)](examples/002-dotnet-WebClient)

* **Microsoft.KernelMemory.SemanticKernelPlugin:** a Memory plugin for Semantic Kernel,
  replacing the original Semantic Memory available in SK. .NET Standard 2.0 compatible.

  [![Nuget package](https://img.shields.io/nuget/vpre/Microsoft.KernelMemory.SemanticKernelPlugin)](https://www.nuget.org/packages/Microsoft.KernelMemory.SemanticKernelPlugin/)
  [![Example code](https://img.shields.io/badge/example-code-blue)](examples/011-dotnet-using-MemoryPlugin)

* **Microsoft.KernelMemory.Abstractions:** The internal interfaces and models
  shared by all packages, used to extend KM to support third party services.
  .NET Standard 2.0 compatible.

  [![Nuget package](https://img.shields.io/nuget/v/Microsoft.KernelMemory.Abstractions)](https://www.nuget.org/packages/Microsoft.KernelMemory.Abstractions/)

* **Microsoft.KernelMemory.MemoryDb.AzureAISearch:** Memory storage using
  **[Azure AI Search](extensions/AzureAISearch)**.

  [![Nuget package](https://img.shields.io/nuget/v/Microsoft.KernelMemory.MemoryDb.AzureAISearch)](https://www.nuget.org/packages/Microsoft.KernelMemory.MemoryDb.AzureAISearch/)

* **Microsoft.KernelMemory.MemoryDb.Postgres:** Memory storage using
  **[PostgreSQL](extensions/Postgres)**.

  [![Nuget package](https://img.shields.io/nuget/v/Microsoft.KernelMemory.MemoryDb.Postgres)](https://www.nuget.org/packages/Microsoft.KernelMemory.MemoryDb.Postgres/)

* **Microsoft.KernelMemory.MemoryDb.Qdrant:** Memory storage using
  **[Qdrant](extensions/Qdrant)**.

  [![Nuget package](https://img.shields.io/nuget/v/Microsoft.KernelMemory.MemoryDb.Qdrant)](https://www.nuget.org/packages/Microsoft.KernelMemory.MemoryDb.Qdrant/)

* **Microsoft.KernelMemory.AI.AzureOpenAI:** Integration with **[Azure OpenAI](extensions/OpenAI)** LLMs.

  [![Nuget package](https://img.shields.io/nuget/v/Microsoft.KernelMemory.AI.AzureOpenAI)](https://www.nuget.org/packages/Microsoft.KernelMemory.AI.AzureOpenAI/)

* **Microsoft.KernelMemory.AI.LlamaSharp:** Integration with **[LLama](extensions/LlamaSharp)** LLMs.

  [![Nuget package](https://img.shields.io/nuget/v/Microsoft.KernelMemory.AI.LlamaSharp)](https://www.nuget.org/packages/Microsoft.KernelMemory.AI.LlamaSharp/)

* **Microsoft.KernelMemory.AI.OpenAI:** Integration with **[OpenAI](extensions/OpenAI)** LLMs.

  [![Nuget package](https://img.shields.io/nuget/v/Microsoft.KernelMemory.AI.OpenAI)](https://www.nuget.org/packages/Microsoft.KernelMemory.AI.OpenAI/)

* **Microsoft.KernelMemory.DataFormats.AzureAIDocIntel:** Integration with
  [Azure AI Document Intelligence](extensions/AzureAIDocIntel).

  [![Nuget package](https://img.shields.io/nuget/v/Microsoft.KernelMemory.DataFormats.AzureAIDocIntel)](https://www.nuget.org/packages/Microsoft.KernelMemory.DataFormats.AzureAIDocIntel/)

* **Microsoft.KernelMemory.Orchestration.AzureQueues:** Ingestion and synthetic memory
  pipelines via [Azure Queue Storage](extensions/AzureQueues).

  [![Nuget package](https://img.shields.io/nuget/v/Microsoft.KernelMemory.Orchestration.AzureQueues)](https://www.nuget.org/packages/Microsoft.KernelMemory.Orchestration.AzureQueues/)

* **Microsoft.KernelMemory.Orchestration.RabbitMQ:** Ingestion and synthetic memory
  pipelines via [RabbitMQ](extensions/RabbitMQ).

  [![Nuget package](https://img.shields.io/nuget/v/Microsoft.KernelMemory.Orchestration.RabbitMQ)](https://www.nuget.org/packages/Microsoft.KernelMemory.Orchestration.RabbitMQ/)

* **Microsoft.KernelMemory.ContentStorage.AzureBlobs:** Used to store content on
  [Azure Storage Blobs](extensions/AzureBlobs).

  [![Nuget package](https://img.shields.io/nuget/v/Microsoft.KernelMemory.ContentStorage.AzureBlobs)](https://www.nuget.org/packages/Microsoft.KernelMemory.ContentStorage.AzureBlobs/)

* **Microsoft.KernelMemory.Core:** The core library, can be used to build custom
  pipelines and handlers, and contains a serverless client to use memory in a
  synchronous way, without the web service. .NET 6+.

  [![Nuget package](https://img.shields.io/nuget/vpre/Microsoft.KernelMemory.Core)](https://www.nuget.org/packages/Microsoft.KernelMemory.Core/)
  [![Example code](https://img.shields.io/badge/example-code-blue)](examples/001-dotnet-Serverless)

### Packages for Python, Java and other languages

Kernel Memory service offers a **Web API** out of the box, including the **OpenAPI
swagger** documentation that you can leverage to test the API and create custom
web clients. For instance, after starting the service locally, see http://127.0.0.1:9001/swagger/index.html.

A .NET Web Client and a Semantic Kernel plugin are available, see the nugets packages above.

A python package with a Web Client and Semantic Kernel plugin will soon be available.
We also welcome PR contributions to support more languages.

# Contributors

<!--
githubcontrib --repo kernel-memory --owner microsoft --showlogin true --sortBy login --cols 6 --imagesize 110
-->

[<img alt="afederici75" src="https://avatars.githubusercontent.com/u/13766049?v=4&s=110" width="110">](https://github.com/afederici75) |[<img alt="alexibraimov" src="https://avatars.githubusercontent.com/u/59023460?v=4&s=110" width="110">](https://github.com/alexibraimov) |[<img alt="alkampfergit" src="https://avatars.githubusercontent.com/u/358545?v=4&s=110" width="110">](https://github.com/alkampfergit) |[<img alt="amomra" src="https://avatars.githubusercontent.com/u/11981363?v=4&s=110" width="110">](https://github.com/amomra) |[<img alt="anthonypuppo" src="https://avatars.githubusercontent.com/u/6828951?v=4&s=110" width="110">](https://github.com/anthonypuppo) |[<img alt="cherchyk" src="https://avatars.githubusercontent.com/u/1703275?v=4&s=110" width="110">](https://github.com/cherchyk) |
:---: |:---: |:---: |:---: |:---: |:---: |
[afederici75](https://github.com/afederici75) |[alexibraimov](https://github.com/alexibraimov) |[alkampfergit](https://github.com/alkampfergit) |[amomra](https://github.com/amomra) |[anthonypuppo](https://github.com/anthonypuppo) |[cherchyk](https://github.com/cherchyk) |

[<img alt="crickman" src="https://avatars.githubusercontent.com/u/66376200?v=4&s=110" width="110">](https://github.com/crickman) |[<img alt="dluc" src="https://avatars.githubusercontent.com/u/371009?v=4&s=110" width="110">](https://github.com/dluc) |[<img alt="DM-98" src="https://avatars.githubusercontent.com/u/10290906?v=4&s=110" width="110">](https://github.com/DM-98) |[<img alt="GraemeJones104" src="https://avatars.githubusercontent.com/u/79144786?v=4&s=110" width="110">](https://github.com/GraemeJones104) |[<img alt="kbeaugrand" src="https://avatars.githubusercontent.com/u/9513635?v=4&s=110" width="110">](https://github.com/kbeaugrand) |[<img alt="lecramr" src="https://avatars.githubusercontent.com/u/20584823?v=4&s=110" width="110">](https://github.com/lecramr) |
:---: |:---: |:---: |:---: |:---: |:---: |
[crickman](https://github.com/crickman) |[dluc](https://github.com/dluc) |[DM-98](https://github.com/DM-98) |[GraemeJones104](https://github.com/GraemeJones104) |[kbeaugrand](https://github.com/kbeaugrand) |[lecramr](https://github.com/lecramr) |

[<img alt="luismanez" src="https://avatars.githubusercontent.com/u/9392197?v=4&s=110" width="110">](https://github.com/luismanez) |[<img alt="marcominerva" src="https://avatars.githubusercontent.com/u/3522534?v=4&s=110" width="110">](https://github.com/marcominerva) |[<img alt="pascalberger" src="https://avatars.githubusercontent.com/u/2190718?v=4&s=110" width="110">](https://github.com/pascalberger) |[<img alt="pawarsum12" src="https://avatars.githubusercontent.com/u/136417839?v=4&s=110" width="110">](https://github.com/pawarsum12) |[<img alt="qihangnet" src="https://avatars.githubusercontent.com/u/1784873?v=4&s=110" width="110">](https://github.com/qihangnet) |[<img alt="slapointe" src="https://avatars.githubusercontent.com/u/1054412?v=4&s=110" width="110">](https://github.com/slapointe) |
:---: |:---: |:---: |:---: |:---: |:---: |
[luismanez](https://github.com/luismanez) |[marcominerva](https://github.com/marcominerva) |[pascalberger](https://github.com/pascalberger) |[pawarsum12](https://github.com/pawarsum12) |[qihangnet](https://github.com/qihangnet) |[slapointe](https://github.com/slapointe) |

[<img alt="slorello89" src="https://avatars.githubusercontent.com/u/42971704?v=4&s=110" width="110">](https://github.com/slorello89) |[<img alt="TaoChenOSU" src="https://avatars.githubusercontent.com/u/12570346?v=4&s=110" width="110">](https://github.com/TaoChenOSU) |[<img alt="teresaqhoang" src="https://avatars.githubusercontent.com/u/125500434?v=4&s=110" width="110">](https://github.com/teresaqhoang) |[<img alt="vicperdana" src="https://avatars.githubusercontent.com/u/7114832?v=4&s=110" width="110">](https://github.com/vicperdana) |[<img alt="xbotter" src="https://avatars.githubusercontent.com/u/3634877?v=4&s=110" width="110">](https://github.com/xbotter) |
:---: |:---: |:---: |:---: |:---: |
[slorello89](https://github.com/slorello89) |[TaoChenOSU](https://github.com/TaoChenOSU) |[teresaqhoang](https://github.com/teresaqhoang) |[vicperdana](https://github.com/vicperdana) |[xbotter](https://github.com/xbotter) |





