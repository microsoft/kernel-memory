// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Evaluation;
using Microsoft.SemanticKernel;

var memoryConfiguration = new KernelMemoryConfig();

var openAIConfig = new OpenAIConfig();
var azureOpenAITextConfig = new AzureOpenAIConfig();
var azureOpenAIEmbeddingConfig = new AzureOpenAIConfig();
var llamaConfig = new LlamaSharpConfig();
var searchClientConfig = new SearchClientConfig();
var azDocIntelConfig = new AzureAIDocIntelConfig();
var azureAISearchConfig = new AzureAISearchConfig();
var postgresConfig = new PostgresConfig();

new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.development.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build()
    .BindSection("KernelMemory", memoryConfiguration)
    .BindSection("KernelMemory:Services:OpenAI", openAIConfig)
    .BindSection("KernelMemory:Services:AzureOpenAIText", azureOpenAITextConfig)
    .BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAIEmbeddingConfig)
    .BindSection("KernelMemory:Services:LlamaSharp", llamaConfig)
    .BindSection("KernelMemory:Services:AzureAIDocIntel", azDocIntelConfig)
    .BindSection("KernelMemory:Services:AzureAISearch", azureAISearchConfig)
    .BindSection("KernelMemory:Services:Postgres", postgresConfig)
    .BindSection("KernelMemory:Retrieval:SearchClient", searchClientConfig);

var memoryBuilder = new KernelMemoryBuilder()
    .AddSingleton(memoryConfiguration)
    // .WithOpenAIDefaults(Environment.GetEnvironmentVariable("OPENAI_API_KEY")) // Use OpenAI for text generation and embedding
    // .WithOpenAI(openAIConfig)                                    // Use OpenAI for text generation and embedding
    // .WithLlamaTextGeneration(llamaConfig)                        // Generate answers and summaries using LLama
    // .WithAzureAISearchMemoryDb(azureAISearchConfig)              // Store memories in Azure AI Search
    // .WithPostgresMemoryDb(postgresConfig)                        // Store memories in Postgres
    // .WithQdrantMemoryDb("http://127.0.0.1:6333")                 // Store memories in Qdrant
    // .WithAzureBlobsDocumentStorage(new AzureBlobsConfig {...})   // Store files in Azure Blobs
    // .WithSimpleVectorDb(SimpleVectorDbConfig.Persistent)         // Store memories on disk
    // .WithSimpleFileStorage(SimpleFileStorageConfig.Persistent)   // Store files on disk
    .WithAzureOpenAITextGeneration(azureOpenAITextConfig)
    .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig);

var kernel = Kernel.CreateBuilder()
    // For OpenAI:
    .AddOpenAIChatCompletion(
        modelId: "gpt-4",
        apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY")!)
    .Build();

var testSetGenerator = new TestSetGeneratorBuilder(memoryBuilder.Services)
    .AddEvaluatorKernel(kernel)
    .Build();

var distribution = new Distribution
{
    Simple = .5f,
    Reasoning = .16f,
    MultiContext = .17f,
    Conditioning = .17f
};

var testSet = testSetGenerator.GenerateTestSetsAsync(index: "default", count: 10, retryCount: 3, distribution: distribution);

await foreach (var test in testSet)
{
    Console.WriteLine(test.Question);
}

var evaluation = new TestSetEvaluatorBuilder()
    .AddEvaluatorKernel(kernel)
    .WithMemory(memoryBuilder.Build())
    .Build();

var results = evaluation.EvaluateTestSetAsync(index: "default", await testSet.ToArrayAsync());

await foreach (var result in results)
{
    Console.WriteLine($"Faithfulness: {result.Metrics.Faithfulness}, ContextRecall: {result.Metrics.ContextRecall}");
}
