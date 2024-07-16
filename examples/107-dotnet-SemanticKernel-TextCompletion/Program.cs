// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var endpoint = Environment.GetEnvironmentVariable("AOAI_ENDPOINT")!;
var apiKey = Environment.GetEnvironmentVariable("AOAI_API_KEY")!;
var chatDeployment = Environment.GetEnvironmentVariable("AOAI_DEPLOYMENT_CHAT")!;
var embeddingDeployment = Environment.GetEnvironmentVariable("AOAI_DEPLOYMENT_EMBEDDING")!;

var config = new SemanticKernelConfig();

var memory = new KernelMemoryBuilder()
    .WithSemanticKernelTextGenerationService(
        new AzureOpenAIChatCompletionService(chatDeployment, endpoint, apiKey), config)
    .WithSemanticKernelTextEmbeddingGenerationService(
        new AzureOpenAITextEmbeddingGenerationService(embeddingDeployment, endpoint, apiKey), config)
    .Build<MemoryServerless>();

await memory.ImportWebPageAsync("https://raw.githubusercontent.com/microsoft/kernel-memory/main/COMMUNITY.md", documentId: "doc001");

var question = "How can I join Kernel Memory's Discord?";
Console.WriteLine($"\n\nQuestion: {question}");

var answer = await memory.AskAsync(question);
Console.WriteLine($"\nAnswer: {answer.Result}");

Console.WriteLine("\n\n  Sources:\n");
foreach (var x in answer.RelevantSources)
{
    Console.WriteLine($"  - {x.SourceName}  - {x.Link} [{x.Partitions.First().LastUpdate:D}]");
}
