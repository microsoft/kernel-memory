﻿// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;

/* The following code shows how to create a custom handler and run it as a standalone service.
 * The handler will automatically attach to a queue and listen for work to do.
 * You can also add multiple handlers the same way.
 */

// Usual .NET web app builder
var appBuilder = WebApplication.CreateBuilder();

/* ... setup your handler dependencies ... */
// builder.Services.AddSingleton...
// builder.Services.AddTransient...

// Define the handlers to host
appBuilder.Services.AddHandlerAsHostedService<MyHandler>("mypipelinestep");
// builder.Services.AddHandlerAsHostedService<MyHandler2>("mypipelinestep-2");
// builder.Services.AddHandlerAsHostedService<MyHandler3>("mypipelinestep-3");

// Build the memory instance injecting its dependencies into the current app
var _ = new KernelMemoryBuilder(appBuilder.Services)
    .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"))
    .Build();

// Build and run .NET web app as usual
Console.WriteLine("Starting service...");
var app = appBuilder.Build();
app.Run();

Console.WriteLine("Service stopped");
