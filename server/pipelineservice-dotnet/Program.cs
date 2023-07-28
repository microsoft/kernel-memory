// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticMemory.Core.Handlers;
using Microsoft.SemanticMemory.InteractiveSetup;

if (new[] { "setup", "-setup" }.Contains(args.FirstOrDefault(), StringComparer.OrdinalIgnoreCase))
{
    Main.InteractiveSetup(cfgWebService: false, cfgSearch: false);
}

var builder = HostedHandlersBuilder.CreateApplicationBuilder();

builder.Services.UseHandlerAsHostedService<TextExtractionHandler>("extract");
builder.Services.UseHandlerAsHostedService<TextPartitioningHandler>("partition");
builder.Services.UseHandlerAsHostedService<GenerateEmbeddingsHandler>("gen_embeddings");
builder.Services.UseHandlerAsHostedService<SaveEmbeddingsHandler>("save_embeddings");

var app = builder.Build();

app.Run();
