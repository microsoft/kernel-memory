// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticKernel.SemanticMemory.Core.Handlers;

var builder = HostedHandlersBuilder.CreateApplicationBuilder();

builder.Services.UseHandlerAsHostedService<TextExtractionHandler>("extract");
builder.Services.UseHandlerAsHostedService<TextPartitioningHandler>("partition");
builder.Services.UseHandlerAsHostedService<GenerateEmbeddingsHandler>("gen_embeddings");
builder.Services.UseHandlerAsHostedService<SaveEmbeddingsHandler>("save_embeddings");

var app = builder.Build();

app.Run();
