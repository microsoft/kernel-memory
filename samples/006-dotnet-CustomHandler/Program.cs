// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.Core.AppBuilders;

var builder = HostedHandlersBuilder.CreateApplicationBuilder();

/* ... setup your handler dependencies ... */
// builder.Services.AddSingleton...
//builder.Services.AddTransient...

builder.Services.AddHandlerAsHostedService<MyHandler>("mypipelinestep");
// builder.Services.AddHandlerAsHostedService<MyHandler2>("mypipelinestep-2");
// builder.Services.AddHandlerAsHostedService<MyHandler3>("mypipelinestep-3");

var app = builder.Build();
app.Run();
