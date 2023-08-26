// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory;

/* The following code shows how to create a custom handler, attached
 * to a queue and listening for work to do. You can also add multiple handlers
 * the same way.
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

// Inject memory dependencies

var _ = new MemoryClientBuilder(appBuilder).WithOpenAIDefaults(Env.Var("OPENAI_API_KEY")).Complete();

// Build and run .NET web app as usual
var app = appBuilder.Build();
app.Run();
