// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;

var memory = new KernelMemoryBuilder()
    .WithOpenAIDefaults(Environment.GetEnvironmentVariable("OPENAI_API_KEY")!)
    // .WithAzureBlobsStorage(new AzureBlobsConfig {...})                                       => use Azure Blobs
    // .WithAzureAISearchMemoryDb(Environment.GetEnvironmentVariable("AZSEARCH_ENDPOINT"), Environment.GetEnvironmentVariable("AZSEARCH_API_KEY"))    => use Azure AI Search
    // .WithQdrantMemoryDb("http://127.0.0.1:6333")                                             => use Qdrant docker
    // .WithAzureAIDocIntel(Environment.GetEnvironmentVariable("AZDOCINTEL_ENDPOINT"), Environment.GetEnvironmentVariable("AZDOCINTEL_API_KEY"))      => use Azure AI Document Intelligence OCR
    // .WithAzureOpenAIEmbeddingGeneration(new AzureOpenAIConfig                                => use Azure OpenAI for embedding generation
    // {
    //     APIType = AzureOpenAIConfig.APITypes.EmbeddingGeneration,
    //     Auth = AzureOpenAIConfig.AuthTypes.AzureIdentity,
    //     Endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"),
    //     Deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBED_MODEL")
    // })
    // .WithAzureOpenAITextGeneration(new AzureOpenAIConfig                                     => use Azure OpenAI for text generation
    // {
    //     APIType = AzureOpenAIConfig.APITypes.ChatCompletion,
    //     Auth = AzureOpenAIConfig.AuthTypes.AzureIdentity,
    //     Endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"),
    //     Deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_MODEL")
    // })
    .Build<MemoryServerless>();

await memory.ImportDocumentAsync("sample-KM-Readme.pdf", documentId: "doc001");

var question = "What's Kernel Memory?";

Console.WriteLine($"\n\nQuestion: {question}");

var answer = await memory.AskAsync(question);

Console.WriteLine($"\nAnswer: {answer.Result}");

Console.WriteLine("\n\n  Sources:\n");

foreach (var x in answer.RelevantSources)
{
    Console.WriteLine($"  - {x.SourceName}  - {x.Link} [{x.Partitions.First().LastUpdate:D}]");
}
