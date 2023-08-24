// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory;

var memory = new MemoryClientBuilder()
    .WithFilesystemStorage("tmp-storage")
    .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"))
    // .WithAzureOpenAIEmbeddingGeneration(new AzureOpenAIConfig
    // {
    //     APIType = AzureOpenAIConfig.APITypes.EmbeddingGeneration,
    //     Auth = AzureOpenAIConfig.AuthTypes.AzureIdentity,
    //     Endpoint = Env.Var("AZURE_OPENAI_ENDPOINT"),
    //     Deployment = Env.Var("AZURE_OPENAI_EMBED_MODEL")
    // })
    // .WithAzureOpenAITextCompletion(new AzureOpenAIConfig
    // {
    //     APIType = AzureOpenAIConfig.APITypes.ChatCompletion,
    //     Auth = AzureOpenAIConfig.AuthTypes.AzureIdentity,
    //     Endpoint = Env.Var("AZURE_OPENAI_ENDPOINT"),
    //     Deployment = Env.Var("AZURE_OPENAI_CHAT_MODEL")
    // })
    .WithAzureCognitiveSearch(Env.Var("ACS_ENDPOINT"), Env.Var("ACS_API_KEY"))
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
