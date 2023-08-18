// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.Core.AI.OpenAI;
using Microsoft.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.ContentStorage.FileSystem;
using Microsoft.SemanticMemory.Core.MemoryStorage.AzureCognitiveSearch;

// Usual .NET web app builder
var appBuilder = WebApplication.CreateBuilder();

/* ... setup your handler dependencies ... */
// builder.Services.AddSingleton...
// builder.Services.AddTransient...

// Define the handlers to host
appBuilder.Services.AddHandlerAsHostedService<MyHandler>("mypipelinestep");
// builder.Services.AddHandlerAsHostedService<MyHandler2>("mypipelinestep-2");
// builder.Services.AddHandlerAsHostedService<MyHandler3>("mypipelinestep-3");

// Inject memory dependencies
var _ = new MemoryClientBuilder(appBuilder)
    .WithFilesystemStorage("tmp")
    .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"))
    // .WithQdrant("http://127.0.0.1:6333")
    .WithAzureCognitiveSearch(Env.Var("ACS_ENDPOINT"), Env.Var("ACS_API_KEY"))
    .Complete();

// Build and run .NET web app as usual
var app = appBuilder.Build();
app.Run();
