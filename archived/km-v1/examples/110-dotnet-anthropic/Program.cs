// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.Anthropic;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;

var anthropicConfig = new AnthropicConfig();
var azureOpenAIEmbeddingConfig = new AzureOpenAIConfig();

new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.development.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build()
    .BindSection("KernelMemory:Services:Anthropic", anthropicConfig)
    .BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAIEmbeddingConfig);

var memory = new KernelMemoryBuilder()
    // Generate answers using Anthropic
    .WithAnthropicTextGeneration(anthropicConfig)
    // Generate embeddings using Azure OpenAI
    .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig)
    // Persist memory on disk
    .WithSimpleFileStorage(SimpleFileStorageConfig.Persistent)
    .WithSimpleVectorDb(SimpleVectorDbConfig.Persistent)
    .Build<MemoryServerless>();

// Import a document in memory
await memory.ImportDocumentAsync("file5-NASA-news.pdf", "file5-NASA-news.pdf");
Console.WriteLine("Document imported");

// now ask a question
var question = "What is orion?";
var answer = await memory.AskAsync(question);

Console.WriteLine($"Question: {question}");
Console.WriteLine($"Answer: {answer.Result}");
