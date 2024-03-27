// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var endpoint = Env.Var("AOAI_ENDPOINT");
var apiKey = Env.Var("AOAI_API_KEY");
var chatDeployment = Env.Var("AOAI_DEPLOYMENT_CHAT");
var embeddingDeployment = Env.Var("AOAI_DEPLOYMENT_EMBEDDING");

var config = new SemanticKernelConfig();
var tokenizer = new DefaultGPTTokenizer();

var memory = new KernelMemoryBuilder()
    .WithSemanticKernelTextGenerationService(
        new AzureOpenAIChatCompletionService(chatDeployment, endpoint, apiKey), config, tokenizer)
    .WithSemanticKernelTextEmbeddingGenerationService(
        new AzureOpenAITextEmbeddingGenerationService(embeddingDeployment, endpoint, apiKey), config, tokenizer)
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
