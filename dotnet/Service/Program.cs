// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Client.Models;
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
        services.UseSearchClient();
    }
});

var config = app.Services.GetService<SemanticMemoryConfig>()
             ?? throw new ConfigurationException("Configuration is null");

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
                return Results.Accepted($"/upload-status?user={input.UserId}&id={id}",
                    new UploadAccepted { Id = id, UserId = input.UserId, Message = "Upload completed, ingestion started" });
            }
            catch (Exception e)
            {
                return Results.Problem(title: "Upload failed", detail: e.Message, statusCode: 503);
            }
        })
        .Produces<UploadAccepted>(StatusCodes.Status202Accepted);

    // Ask endpoint
    app.MapPost("/ask",
            async Task<IResult> (SearchRequest request, SearchClient searchClient, ILogger<Program> log) =>
            {
                log.LogTrace("New search request");
                MemoryAnswer answer = await searchClient.SearchAsync(request);
                return Results.Ok(answer);
            })
        .Produces<MemoryAnswer>(StatusCodes.Status200OK);

    // Document status endpoint
    app.MapGet("/upload-status",
            async Task<IResult> ([FromQuery(Name = "user")] string userId, [FromQuery(Name = "id")] string pipelineId, IPipelineOrchestrator orchestrator) =>
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.BadRequest("'user' query parameter is missing or has no value");
                }

                if (string.IsNullOrEmpty(pipelineId))
                {
                    return Results.BadRequest("'id' query parameter is missing or has no value");
                }

                DataPipeline? pipeline = await orchestrator.ReadPipelineStatusAsync(userId, pipelineId);
                if (pipeline == null)
                {
                    return Results.NotFound("Document pipeline not found");
                }

                var result = new DataPipelineStatus
                {
                    Completed = pipeline.Complete,
                    Failed = false, // TODO
                    Id = pipeline.Id,
                    UserId = pipeline.UserId,
                    Tags = pipeline.Tags,
                    Creation = pipeline.Creation,
                    LastUpdate = pipeline.LastUpdate,
                    Steps = pipeline.Steps,
                    RemainingSteps = pipeline.RemainingSteps,
                    CompletedSteps = pipeline.CompletedSteps,
                };

                return Results.Ok(result);
            })
        .Produces<MemoryAnswer>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);
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
