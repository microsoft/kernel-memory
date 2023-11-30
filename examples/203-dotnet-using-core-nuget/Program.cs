﻿// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;

var memory = new KernelMemoryBuilder()
    .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"))
    // .FromAppSettings() => read "KernelMemory" settings from appsettings.json (if available), see https://github.com/microsoft/kernel-memory/blob/main/dotnet/Service/appsettings.json as an example
    // .WithAzureBlobsStorage(new AzureBlobsConfig {...})                                       => use Azure Blobs
    // .WithAzureAISearch(Env.Var("AZSEARCH_ENDPOINT"), Env.Var("AZSEARCH_API_KEY"))            => use Azure AI Search
    // .WithQdrant("http://127.0.0.1:6333")                                                     => use Qdrant docker
    // .WithAzureAIDocIntel(Env.Var("AZDOCINTEL_ENDPOINT"), Env.Var("AZDOCINTEL_API_KEY"))      => use Azure AI Document Intelligence OCR
    // .WithAzureOpenAIEmbeddingGeneration(new AzureOpenAIConfig                                => use Azure OpenAI for embedding generation
    // {
    //     APIType = AzureOpenAIConfig.APITypes.EmbeddingGeneration,
    //     Auth = AzureOpenAIConfig.AuthTypes.AzureIdentity,
    //     Endpoint = Env.Var("AZURE_OPENAI_ENDPOINT"),
    //     Deployment = Env.Var("AZURE_OPENAI_EMBED_MODEL")
    // })
    // .WithAzureOpenAITextGeneration(new AzureOpenAIConfig                                     => use Azure OpenAI for text generation
    // {
    //     APIType = AzureOpenAIConfig.APITypes.ChatCompletion,
    //     Auth = AzureOpenAIConfig.AuthTypes.AzureIdentity,
    //     Endpoint = Env.Var("AZURE_OPENAI_ENDPOINT"),
    //     Deployment = Env.Var("AZURE_OPENAI_CHAT_MODEL")
    // })
    .BuildServerlessClient();

await memory.ImportDocumentAsync("sample-SK-Readme.pdf", documentId: "doc001");

var question = "What's Semantic Kernel?";

Console.WriteLine($"\n\nQuestion: {question}");

var answer = await memory.AskAsync(question);

Console.WriteLine($"\nAnswer: {answer.Result}");

Console.WriteLine("\n\n  Sources:\n");

foreach (var x in answer.RelevantSources)
{
    Console.WriteLine($"  - {x.SourceName}  - {x.Link} [{x.Partitions.First().LastUpdate:D}]");
}
