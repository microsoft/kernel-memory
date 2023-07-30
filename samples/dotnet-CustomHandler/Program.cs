// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.Core.AppBuilders;

var builder = HostedHandlersBuilder.CreateApplicationBuilder();

builder.Services.UseHandlerAsHostedService<MyHandler>("mypipelinestep");
// builder.Services.UseHandlerAsHostedService<MyHandler2>("mypipelinestep-2");
// builder.Services.UseHandlerAsHostedService<MyHandler3>("mypipelinestep-3");

var app = builder.Build();
app.Run();
