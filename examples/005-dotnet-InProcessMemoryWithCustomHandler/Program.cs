// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory;

var memory = new MemoryClientBuilder()
    .WithFilesystemStorage("tmp-storage")
    .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"))
    // .WithQdrant("http://127.0.0.1:6333")
    .WithAzureCognitiveSearch(Env.Var("ACS_ENDPOINT"), Env.Var("ACS_API_KEY"))
    .BuildServerlessClient();

memory.AddHandler(new MyHandler("my_step", memory.Orchestrator));

await memory.ImportDocumentAsync("sample-Wikipedia-Moon.txt", steps: new[] { "my_step" });

/* Output:
 * My handler is working
 */
