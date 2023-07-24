// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel.SemanticMemory.Core;
using Microsoft.SemanticKernel.SemanticMemory.Core.Handlers;
using Microsoft.SemanticKernel.Services.SemanticMemory.PipelineService;

var builder = HostedHandlersBuilder.CreateApplicationBuilder();
builder.Services.UseDefaultHandler<TextExtractionHandler>("extract");
builder.Services.UseDefaultHandler<TextPartitioningHandler>("partition");
builder.Services.UseDefaultHandler<GenerateEmbeddingsHandler>("gen_embeddings");
// builder.Services.UseDefaultHandler<SaveEmbeddingsHandler>("save_embeddings");

builder.AddHandler<TextExtraction>("extract");
builder.AddHandler<TextPartitioning>("partition");
builder.AddHandler<GenerateEmbeddings>("gen_embeddings");
// builder.AddHandler<GenerateEmbeddings>("save_embeddings");

var app = builder.Build();

app.Run();
