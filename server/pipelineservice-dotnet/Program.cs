// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel.SemanticMemory.Core;
using Microsoft.SemanticKernel.SemanticMemory.Core.Handlers;
using Microsoft.SemanticKernel.Services.SemanticMemory.PipelineService;

var builder = HostedHandlersBuilder.CreateApplicationBuilder();
builder.Services.UseDefaultHandler<TextExtractionHandler>("extract");
builder.Services.UseDefaultHandler<TextPartitioningHandler>("partition");

builder.AddHandler<TextExtraction>("extract");
builder.AddHandler<TextPartitioning>("partition");
// builder.AddHandler<IndexingHandler>("index"); // work in progress

var app = builder.Build();

app.Run();
