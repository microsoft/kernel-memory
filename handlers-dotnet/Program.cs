// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel.Services.Configuration;
using Microsoft.SemanticKernel.Services.SemanticMemory.Handlers;

var builder = HostedHandlersBuilder.CreateApplicationBuilder();

builder.AddHandler<TextExtractionHandler>("extract");
// builder.AddHandler<TextPartitioningHandler>("partition"); // work in progress
// builder.AddHandler<IndexingHandler>("index"); // work in progress

var app = builder.Build();

app.Run();
