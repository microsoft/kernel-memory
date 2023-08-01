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
using Microsoft.SemanticMemory.Core.Search;
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

var app = AppBuilder.Build((services, config) =>
{
    if (config.Service.RunHandlers)
    {
        // Register pipeline handlers as hosted services
        services.UseHandlerAsHostedService<TextExtractionHandler>("extract");
        services.UseHandlerAsHostedService<TextPartitioningHandler>("partition");
        services.UseHandlerAsHostedService<GenerateEmbeddingsHandler>("gen_embeddings");
        services.UseHandlerAsHostedService<SaveEmbeddingsHandler>("save_embeddings");
    }

    if (config.Service.RunWebService && config.OpenApiEnabled)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }

    if (config.Service.RunWebService)
    {
        services.UseSearchClient(config);
    }
});

var config = app.Services.GetService<SemanticMemoryConfig>();

// ********************************************************
// ************** WEB SERVICE ENDPOINTS *******************
// ********************************************************

#pragma warning disable CA2254 // The log msg template should be a constant
#pragma warning disable CA1031 // Catch all required to control all errors
// ReSharper disable once TemplateIsNotCompileTimeConstantProblem

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
        ILogger<Program> log) =>
    {
        log.LogTrace("New upload request");

        // Note: .NET doesn't yet support binding multipart forms including data and files
        (UploadRequest input, bool isValid, string errMsg) = await UploadRequest.BindHttpRequestAsync(request).ConfigureAwait(false);

        if (!isValid)
        {
            log.LogError(errMsg);
            return Results.BadRequest(errMsg);
        }

        try
        {
            var id = await orchestrator.UploadFileAsync(input);
            return Results.Accepted($"/upload-status?id={id}", new { Id = id, Message = "Upload completed, ingestion started" });
        }
        catch (Exception e)
        {
            return Results.Problem(title: "Upload failed", detail: e.Message, statusCode: 503);
        }
    });

    // Ask endpoint
    app.MapPost("/ask", async Task<IResult> (
        SearchRequest request,
        SearchClient searchClient,
        ILogger<Program> log) =>
    {
        log.LogTrace("New search request");
        return Results.Ok(await searchClient.SearchAsync(request));
    });
}
#pragma warning restore CA1031
#pragma warning restore CA2254

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
