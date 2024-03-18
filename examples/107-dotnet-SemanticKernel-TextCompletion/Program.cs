// Copyright (c) Microsoft. All rights reserved.

#if KernelMemoryDev // This code requires the next release (use KernelMemoryDev.sln)
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.OpenAI;
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

await memory.ImportDocumentAsync("story.docx", documentId: "doc001");

var question = "What's radiant mold?";
Console.WriteLine($"\n\nQuestion: {question}");

var answer = await memory.AskAsync(question);
Console.WriteLine($"\nAnswer: {answer.Result}");

Console.WriteLine("\n\n  Sources:\n");
foreach (var x in answer.RelevantSources)
{
    Console.WriteLine($"  - {x.SourceName}  - {x.Link} [{x.Partitions.First().LastUpdate:D}]");
}

#endif
