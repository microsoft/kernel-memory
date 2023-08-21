// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.Core.AI.OpenAI;
using Microsoft.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.ContentStorage.FileSystem;
using Microsoft.SemanticMemory.Core.MemoryStorage.AzureCognitiveSearch;

var memory = new MemoryClientBuilder()
    .WithFilesystemStorage("tmp")
    .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"))
    // .WithQdrant("http://127.0.0.1:6333")
    .WithAzureCognitiveSearch(Env.Var("ACS_ENDPOINT"), Env.Var("ACS_API_KEY"))
    .BuildServerlessClient();

memory.AddHandler(new MyHandler("my_step", memory.Orchestrator));

await memory.ImportDocumentAsync("sample-Wikipedia-Moon.txt", steps: new[] { "my_step" });

/* Output:
 * My handler is working
 */
