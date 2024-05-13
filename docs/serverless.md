---
nav_order: 15
has_children: true
title: Serverless (.NET)
permalink: /serverless
layout: default
---
# Serverless memory (.NET only)

Kernel Memory works and scales at best when running as a service, allowing to
ingest thousands of documents and information without blocking your app. However,
you can also embed the `MemoryServerless` class in your app, using `KernelMemoryBuilder`.

**`MemoryServerless` and `MemoryWebClient` implement the same interface
and offer the same API**, so you can easily switch from one to the other.

> {: .important }
The embedded serverless mode is available only for .NET applications, by
running the KM codebase inside the same process of your .NET application.

> {: .warning }
By default the serverless memory is **volatile** and keeps all data only in memory,
without persistence on disk. Follow the configuration instructions to persist memory
across multiple executions.





> ### Importing documents into your Kernel Memory can be as simple as this:
>
> ```csharp
> var memory = new KernelMemoryBuilder()
>     .WithOpenAIDefaults(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
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

# Topics