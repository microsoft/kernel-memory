// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.Diagnostics;
using Microsoft.SemanticMemory.Core.Handlers;
using Microsoft.SemanticMemory.Core.Pipeline;
using Microsoft.SemanticMemory.Core.WebService;
using Microsoft.SemanticMemory.InteractiveSetup;

// ********************************************************
// ************** APP SETTINGS ****************************
// ********************************************************

// Run `dotnet run setup` to run this code and setup the service
if (new[] { "setup", "-setup" }.Contains(args.FirstOrDefault(), StringComparer.OrdinalIgnoreCase))
{
    Main.InteractiveSetup(cfgService: true);
}

// ********************************************************
// ************** APP BUILD *******************************
// ********************************************************

var builder = WebApplication.CreateBuilder(args);

SemanticMemoryConfig config = builder.Services.UseConfiguration(builder.Configuration);

builder.Logging.ConfigureLogger();
builder.Services.UseContentStorage(config);
builder.Services.UseOrchestrator(config);

if (config.Service.RunHandlers)
{
    // Register pipeline handlers as hosted services
    builder.Services.UseHandlerAsHostedService<TextExtractionHandler>("extract");
    builder.Services.UseHandlerAsHostedService<TextPartitioningHandler>("partition");
    builder.Services.UseHandlerAsHostedService<GenerateEmbeddingsHandler>("gen_embeddings");
    builder.Services.UseHandlerAsHostedService<SaveEmbeddingsHandler>("save_embeddings");
}

if (config.Service.RunWebService && config.OpenApiEnabled)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

WebApplication app = builder.Build();

// ********************************************************
// ************** WEB SERVICE ENDPOINTS *******************
// ********************************************************

if (config.Service.RunWebService)
{
    if (config.OpenApiEnabled)
    {
        // URL: http://localhost:9001/swagger/index.html
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    DateTimeOffset start = DateTimeOffset.UtcNow;

    // Simple ping endpoint
    app.MapGet("/", () => Results.Ok("Ingestion service is running. " +
                                     "Uptime: " + (DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                                                   - start.ToUnixTimeSeconds()) + " secs"));
    // File upload endpoint
    app.MapPost("/upload", async Task<IResult> (
        HttpRequest request,
        IPipelineOrchestrator orchestrator,
        ILogger<Program> log) => await Endpoints.UploadAsync(app, request, orchestrator, log));
}

// ********************************************************
// ************** START ***********************************
// ********************************************************

app.Logger.LogInformation(
    "Starting Semantic Memory service, .NET Env: {0}, Log Level: {1}, Web service: {2}, Pipeline handlers: {3}, Orchestration: {4}",
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
    app.Logger.GetLogLevelName(),
    config.Service.RunWebService,
    config.Service.RunHandlers,
    config.Orchestration.Type);

app.Run();
