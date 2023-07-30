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
using Microsoft.SemanticMemory.Core.Pipeline;
using Microsoft.SemanticMemory.Core.WebService;
using Microsoft.SemanticMemory.InteractiveSetup;

// ********************************************************
// ************** APP SETTINGS ****************************
// ********************************************************

if (new[] { "setup", "-setup" }.Contains(args.FirstOrDefault(), StringComparer.OrdinalIgnoreCase))
{
    Main.InteractiveSetup(cfgOrchestration: false, cfgHandlers: false);
}

// ********************************************************
// ************** APP BUILD *******************************
// ********************************************************

var builder = WebApplication.CreateBuilder(args);

SemanticMemoryConfig config = builder.Services.UseConfiguration(builder.Configuration);

builder.Logging.ConfigureLogger();
builder.Services.UseContentStorage(config);
builder.Services.UseOrchestrator(config);

if (config.OpenApiEnabled)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

WebApplication app = builder.Build();

if (config.OpenApiEnabled)
{
    // URL: http://localhost:9001/swagger/index.html
    app.UseSwagger();
    app.UseSwaggerUI();
}

DateTimeOffset start = DateTimeOffset.UtcNow;

// ********************************************************
// ************** ENDPOINTS *******************************
// ********************************************************

// Simple ping endpoint
app.MapGet("/", () => Results.Ok("Ingestion service is running. " +
                                 "Uptime: " + (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - start.ToUnixTimeSeconds()) + " secs"));

// File upload endpoint
app.MapPost("/upload", async Task<IResult> (
    HttpRequest request,
    IPipelineOrchestrator orchestrator,
    ILogger<Program> log) => await Endpoints.UploadAsync(app, request, orchestrator, log));

app.Logger.LogInformation(
    "Starting web service, Log Level: {0}, .NET Env: {1}, Orchestration: {2}",
    app.Logger.GetLogLevelName(),
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
    config.Orchestration.Type);

app.Run();
