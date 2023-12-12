// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.Tokenizers;

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
        .AddFile("file4-SK-Readme.pdf")
        .AddFile("file5-NASA-news.pdf"),
    steps: Constants.PipelineOnlySummary);

// Fetch the list of summaries. The API returns one summary for each file.
var results = await memory.SearchSummariesAsync(filter: MemoryFilters.ByDocument("doc1"));

// Print the summaries!
foreach (var result in results)
{
    Console.WriteLine($"== {result.SourceName} summary ==\n{result.Partitions.First().Text}\n");
}

// ReSharper disable CommentTypo
/*

OUTPUT:

== file4-SK-Readme.pdf summary ==
Semantic Kernel (SK) is a lightweight SDK that allows integration of AI Large Language Models
with conventional programming languages. It combines natural language semantic functions,
traditional code native functions, and embeddings-based memory. SK supports prompt templifying,
function chaining, vectorized memory, and intelligent planning capabilities. It encapsulates
several design patterns from AI research, enabling developers to infuse their applications
with various plugins. The SK community invites developers to contribute and build AI-first
apps. SK is available for C# and Python and includes sample applications. It requires an Open
AI API Key or Azure Open AI service key to get started. The project welcomes contributions and
suggestions, and it operates under the Microsoft Open Source Code of Conduct.

== file5-NASA-news.pdf summary ==
NASA has invited the media to view the new test version of the Orion spacecraft and the recovery
hardware for the Artemis II mission. The event will be held at Naval Base San Diego on August 2.
Personnel from NASA, the U.S. Navy, and the U.S. Air Force will be available for interviews.
The teams are currently conducting tests in the Pacific Ocean to prepare for the Artemis II
mission, which will send four astronauts around the moon. The Artemis II crew, including NASA
astronauts Reid Wiseman, Victor Glover, and Christina Koch, and Canadian Space Agency astronaut
Jeremy Hansen, will participate in recovery testing at sea next year.

*/
