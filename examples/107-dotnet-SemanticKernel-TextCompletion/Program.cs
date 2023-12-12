// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.Tokenizers;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;

var endpoint = Env.Var("AOAI_ENDPOINT");
var apiKey = Env.Var("AOAI_API_KEY");
var chatDeployment = Env.Var("AOAI_DEPLOYMENT_CHAT");
var embeddingDeployment = Env.Var("AOAI_DEPLOYMENT_EMBEDDING");

var config = new SemanticKernelConfig();
var tokenizer = new DefaultGPTTokenizer();

var memory = new KernelMemoryBuilder()
    .WithSemanticKernelTextGenerationService(new AzureOpenAIChatCompletionService(chatDeployment, "", endpoint, apiKey)
                                            , config
                                            , tokenizer)
    .WithSemanticKernelTextEmbeddingGeneration(new AzureOpenAITextEmbeddingGeneration(embeddingDeployment, "", endpoint, apiKey)
                                            , config
                                            , tokenizer)
    .Build<MemoryServerless>();

// using document form 203-dotnet-using-core-nuget

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
