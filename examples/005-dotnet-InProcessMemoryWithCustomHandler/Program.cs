// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;

var memory = new KernelMemoryBuilder()
    // .FromAppSettings() => read "KernelMemory" settings from appsettings.json (if available), see https://github.com/microsoft/kernel-memory/blob/main/dotnet/Service/appsettings.json as an example
    .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"))
    .BuildServerlessClient();

memory.AddHandler(new MyHandler("my_step", memory.Orchestrator));

await memory.ImportDocumentAsync("sample-Wikipedia-Moon.txt", steps: new[] { "my_step" });

/* Output:
 * My handler is working
 */
