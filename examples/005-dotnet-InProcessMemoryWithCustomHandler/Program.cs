// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory;

var memory = new MemoryClientBuilder().WithOpenAIDefaults(Env.Var("OPENAI_API_KEY")).BuildServerlessClient();

memory.AddHandler(new MyHandler("my_step", memory.Orchestrator));

await memory.ImportDocumentAsync("sample-Wikipedia-Moon.txt", steps: new[] { "my_step" });

/* Output:
 * My handler is working
 */
