// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticKernel.SemanticMemory.Core.Handlers;
using Microsoft.SemanticKernel.SemanticMemory.InteractiveSetup;

if (new[] { "setup", "-setup" }.Contains(args.FirstOrDefault(), StringComparer.OrdinalIgnoreCase))
{
    Setup.InteractiveSetup(cfgWebService: false);
    Environment.Exit(0);
}

var builder = HostedHandlersBuilder.CreateApplicationBuilder();

builder.Services.UseHandlerAsHostedService<TextExtractionHandler>("extract");
builder.Services.UseHandlerAsHostedService<TextPartitioningHandler>("partition");
builder.Services.UseHandlerAsHostedService<GenerateEmbeddingsHandler>("gen_embeddings");
builder.Services.UseHandlerAsHostedService<SaveEmbeddingsHandler>("save_embeddings");

var app = builder.Build();

app.Run();
