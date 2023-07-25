// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticKernel.SemanticMemory.Core.Handlers;

var builder = HostedHandlersBuilder.CreateApplicationBuilder();

builder.UseHandlerAsHostedService<TextExtractionHandler>("extract");
builder.UseHandlerAsHostedService<TextPartitioningHandler>("partition");
builder.UseHandlerAsHostedService<GenerateEmbeddingsHandler>("gen_embeddings");

var app = builder.Build();

app.Run();
