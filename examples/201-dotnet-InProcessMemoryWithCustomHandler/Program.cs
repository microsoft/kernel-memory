// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;

var memory = new KernelMemoryBuilder()
    .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"))
    .Build<MemoryServerless>();

memory.AddHandler(new MyHandler("my_step", memory.Orchestrator));

await memory.ImportDocumentAsync("sample-Wikipedia-Moon.txt", steps: new[] { "my_step" });

/* Output:
 * My handler is working
 */
