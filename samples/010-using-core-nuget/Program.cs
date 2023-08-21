// Copyright (c) Microsoft. All rights reserved.

using dotenv.net;
using Microsoft.SemanticMemory.Core.AI.OpenAI;
using Microsoft.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticMemory.Core.ContentStorage.FileSystem;
using Microsoft.SemanticMemory.Core.MemoryStorage.AzureCognitiveSearch;

DotEnv.Load();
var env = DotEnv.Read();

var memory = new MemoryClientBuilder()
    .WithFilesystemStorage("tmp")
    .WithOpenAIDefaults(env["OPENAI_API_KEY"])
    .WithAzureCognitiveSearch(env["ACS_ENDPOINT"], env["ACS_API_KEY"])
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
