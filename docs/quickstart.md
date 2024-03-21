---
nav_order: 2
has_children: true
title: Quickstart
permalink: /quickstart
layout: default
has_toc: false
---
# Quickstart, local deployment

This guide assumes you're familiar with web services, docker and OpenAI settings.

This guide assumes you have prior knowledge of web services, Docker, and OpenAI settings. In this quickstart tutorial,
we will set up the service and demonstrate how to use the Memory API from Python, .NET, Java and a Bash command line.

## Requirements

* [.NET 6](https://dotnet.microsoft.com/download) or higher
* Either an [OpenAI API Key](https://platform.openai.com/api-keys) or
  [Azure OpenAI deployment](https://azure.microsoft.com/products/ai-services/openai-service). If you are familiar
  with llama.cpp or LLamaSharp you can also use a LLama model. However, this may result in slower AI code execution,
  depending on your device.
* A vector database, such as Azure AI Search, Qdrant, or Postgres+pgvector. For basic tests, you can use KM
  SimpleVectorDb.
* A copy of the [KM repository](https://github.com/microsoft/kernel-memory).

## Next: [Create a configuration file](quickstart/configuration)

# Other examples

The repository contains more documentation and examples, here's some suggestions:

* [KM concepts: Indexes, Documents, Tags and more](concepts)
* [Memory API](functions)
* [Collection of Jupyter notebooks with various scenarios](https://github.com/microsoft/kernel-memory/tree/main/examples/000-notebooks)
* [Using Kernel Memory web service to upload documents and answer questions](https://github.com/microsoft/kernel-memory/tree/main/examples/001-dotnet-WebClient)
* [Summarizing documents](https://github.com/microsoft/kernel-memory/tree/main/examples/106-dotnet-retrieve-synthetics)
