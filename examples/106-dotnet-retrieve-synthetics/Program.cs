// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.OpenAI;

// Use this boolean to decide whether to use OpenAI or Azure OpenAI models
const bool UseAzure = true;

var azureOpenAIEmbeddingConfig = new AzureOpenAIConfig();
var azureOpenAITextConfig = new AzureOpenAIConfig();
var openAIConfig = new OpenAIConfig();

new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build()
    .BindSection("KernelMemory:Services:OpenAI", openAIConfig)
    .BindSection("KernelMemory:Services:AzureOpenAIText", azureOpenAITextConfig)
    .BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAIEmbeddingConfig);

// Note: this example is storing data in memory, so summaries are lost once the program completes.
//       You can customize the code to persist the data, or simply point to a Kernel Memory service.
//var memory = new MemoryWebClient("http://127.0.0.1:9001");
var memory = new KernelMemoryBuilder()
    .Configure(UseAzure,
        builder => builder
            .WithAzureOpenAITextGeneration(azureOpenAITextConfig, new DefaultGPTTokenizer())
            .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig, new DefaultGPTTokenizer()),
        builder => builder.WithOpenAI(openAIConfig))
    .Build<MemoryServerless>();

// Import a couple of documents to summarize.
// Note that we're using a custom set of steps, asking the pipeline to just summarize the docs (ie skipping chunking)
await memory.ImportDocumentAsync(new Document("doc1")
        .AddFile("file4-KM-Readme.pdf")
        .AddFile("file5-NASA-news.pdf"),
    steps: Constants.PipelineOnlySummary);

// Fetch the list of summaries. The API returns one summary for each file.
var results = await memory.SearchSummariesAsync(filter: MemoryFilters.ByDocument("doc1"));

// Print the summaries
foreach (var result in results)
{
    Console.WriteLine($"== {result.SourceName} summary ==\n{result.Partitions.First().Text}\n");
}

// ReSharper disable CommentTypo
/*

OUTPUT:

== file4-KM-Readme.pdf summary ==
Kernel Memory is an AI service designed for efficient indexing of datasets, supporting features like Retrieval Augmented Generation, synthetic memory, and custom semantic memory processing. It integrates with platforms like Semantic Kernel, Microsoft Copilot, and ChatGPT, and is available as a GPT Plugin, web clients, a .NET library, and a Docker container. It allows natural language querying and provides answers with citations from indexed data.
Semantic Memory, part of the Semantic Kernel project, is a library for C#, Python, and Java that supports vector search and wraps database calls. Kernel Memory builds upon this, offering additional features like text extraction from various file formats, secure data frameworks, and a .NET codebase for ease of use across languages and platforms.
Kernel Memory supports a wide range of data formats and backends, including Microsoft Office files, PDFs, web pages, images with OCR, and JSON files. It integrates with various AI and vector storage services and offers document storage and orchestration options.
Kernel Memory can be used in serverless mode, embedded in applications, or as a service for scalable document ingestion and information retrieval. It supports custom ingestion pipelines, data lineage, and citations for verifying answer accuracy.
The service provides a web API with OpenAPI documentation for easy access and testing. It also includes a Docker image for quick deployment and a web client for file import and querying. Custom memory ingestion pipelines can be defined with .NET handlers, and the service offers a range of .NET packages for integration with different services and platforms. Python and Java packages are also planned, with contributions for other languages welcomed.

== file5-NASA-news.pdf summary ==
NASA has announced an event for media to view the new test version of the Orion spacecraft and recovery hardware for the Artemis II mission, scheduled for 11 a.m. PDT on August 2 at Naval Base San Diego. Recovery operations personnel from NASA, the U.S. Navy, and the U.S. Air Force will be available for interviews. U.S. media must RSVP by July 31. The event is part of a series of tests in the Pacific Ocean to prepare for the Artemis II mission, NASA's first crewed flight around the Moon, which will include astronauts Reid Wiseman, Victor Glover, Christina Koch, and CSA astronaut Jeremy Hansen. For more information, contact Rachel Kraft or Madison Tuttle.

*/
