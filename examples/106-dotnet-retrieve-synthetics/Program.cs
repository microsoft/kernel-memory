// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Context;

// Use this boolean to decide whether to use OpenAI or Azure OpenAI models
const bool UseAzure = true;

var azureOpenAIEmbeddingConfig = new AzureOpenAIConfig();
var azureOpenAITextConfig = new AzureOpenAIConfig();
var openAIConfig = new OpenAIConfig();

// Using WebApplicationBuilder to load log config from config files
WebApplicationBuilder appBuilder = WebApplication.CreateBuilder();
appBuilder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.development.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build()
    .BindSection("KernelMemory:Services:OpenAI", openAIConfig)
    .BindSection("KernelMemory:Services:AzureOpenAIText", azureOpenAITextConfig)
    .BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAIEmbeddingConfig);

// Note: this example is storing data in memory, so summaries are lost once the program completes.
//       You can customize the code to persist the data, or simply point to a Kernel Memory service.
//var memory = new MemoryWebClient("http://127.0.0.1:9001");
var memory = new KernelMemoryBuilder(appBuilder.Services)
    .Configure(UseAzure,
        builder => builder
            .WithAzureOpenAITextGeneration(azureOpenAITextConfig)
            .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig),
        builder => builder.WithOpenAI(openAIConfig))
    .Build<MemoryServerless>();

// Let's define a custom prompt to summarize our content, and other params used during
// the summarization, overriding the default KM settings.
var context = new RequestContext();
context.SetArg("custom_summary_prompt_str", "Super extra summarize, use short sentences, no list, no new lines. The answer must be short. Content: {{$input}}. Summary: ");
context.SetArg("custom_summary_target_token_size_int", 15); // Try to generate a token no longer than 15 tokens
context.SetArg("custom_summary_overlapping_tokens_int", 0); // Disable overlapping tokens

// Import a couple of documents to summarize.
// Note that we're using a custom set of steps, asking the pipeline to just summarize the docs (ie skipping chunking)
await memory.ImportDocumentAsync(new Document("doc1")
        .AddFile("file4-KM-Readme.pdf")
        .AddFile("file5-NASA-news.pdf"),
    steps: Constants.PipelineOnlySummary,
    context: context);

// Fetch the list of summaries. The API returns one summary for each file.
var results = await memory.SearchSummariesAsync(filter: MemoryFilters.ByDocument("doc1"));

// Print the summaries
foreach (var result in results)
{
    Console.WriteLine($"== {result.SourceName} summary ==\n{result.Partitions.First().Text}\n");
}

/*

OUTPUT:

== file5-NASA-news.pdf summary ==
NASA invites media to view the Orion recovery craft for Artemis II on Aug. 2 at Naval Base San Diego, showcasing recovery
operations and hardware for the mission that will send four astronauts around the Moon.

== file4-KM-Readme.pdf summary ==
Kernel Memory (KM) is an AI service for efficient dataset indexing and querying, supporting various tools, data formats,
and storage engines. It offers summarization, security filters, OCR, and can run serverless or as a service, with a web
API and .NET packages for integration.
*/
