---
nav_order: 91
has_children: false
title: Packages
permalink: /packages
layout: default
---
# .NET packages

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

A python package with a Web Client and Semantic Kernel plugin will soon be available.
We also welcome PR contributions to support more languages.
