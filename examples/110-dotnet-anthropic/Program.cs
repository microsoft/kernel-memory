// Copyright (c) Microsoft. All rights reserved.

#if KernelMemoryDev
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.Anthropic;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.ContentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;

var azureOpenAIEmbeddingConfig = new AzureOpenAIConfig();

var services = new ServiceCollection();
services.AddHttpClient();

new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddUserSecrets<Program>()
    .Build()
    .BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAIEmbeddingConfig);

var anthropicTextGenerationConfiguration = new AnthropicConfiguration();

anthropicTextGenerationConfiguration.MaxTokenTotal = 2048;
anthropicTextGenerationConfiguration.ApiKey = "";
anthropicTextGenerationConfiguration.TextModelName = AnthropicConfiguration.HaikuModelName;

var memory = new KernelMemoryBuilder(services)
    .WithAnthropicTextGeneration(anthropicTextGenerationConfiguration)
    .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig, new DefaultGPTTokenizer())
    .WithSimpleFileStorage(new SimpleFileStorageConfig()
    {
        Directory = "c:\\temp\\km\\storage",
        StorageType = FileSystemTypes.Disk
    })
    .WithSimpleVectorDb(new SimpleVectorDbConfig()
    {
        Directory = "c:\\temp\\km\\vectorstorage",
        StorageType = FileSystemTypes.Disk
    })
    .Build<MemoryServerless>();

var importDocumentTask = memory.ImportDocumentAsync("file5-NASA-news.pdf", "file5-NASA-news.pdf");

await importDocumentTask;

Console.WriteLine("Imported document");

// now ask a question
var question = "What is orion?";
var answer = await memory.AskAsync(question);

Console.WriteLine($"Question: {question}");
Console.WriteLine($"Answer: {answer.Result}");

#else
Console.WriteLine("KernelMemoryDev.sln required");

#endif
