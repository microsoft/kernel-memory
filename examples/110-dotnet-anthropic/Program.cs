// Copyright (c) Microsoft. All rights reserved.

#if KernelMemoryDev
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.Anthropic;
using Microsoft.KernelMemory.ContentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;

var azureOpenAIEmbeddingConfig = new AzureOpenAIConfig();

var services = new ServiceCollection();
services.AddHttpClient();

var anthropicConfig = new AnthropicConfiguration();

new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddUserSecrets<Program>()
    .Build()
    .BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAIEmbeddingConfig)
    .BindSection("KernelMemory:Services:Anthropic", anthropicConfig);

var memory = new KernelMemoryBuilder(services)
    .WithAnthropicTextGeneration(anthropicConfig)
    .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig)
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

Console.ReadKey();

#else
Console.WriteLine("KernelMemoryDev.sln required");

#endif
