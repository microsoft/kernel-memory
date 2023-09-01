// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory;
using Microsoft.SemanticMemory.Configuration;
using Microsoft.SemanticMemory.Diagnostics;
using Microsoft.SemanticMemory.InteractiveSetup;
using Microsoft.SemanticMemory.WebService;

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

// Usual .NET web app builder
var appBuilder = WebApplication.CreateBuilder();

// OpenAPI/swagger
appBuilder.Services.AddEndpointsApiExplorer();
appBuilder.Services.AddSwaggerGen();

// Inject memory client and its dependencies
// Note: pass the current service collection to the builder, in order to start the pipeline handlers
ISemanticMemoryClient memory = new MemoryClientBuilder(appBuilder.Services).FromAppSettings().Build();
appBuilder.Services.AddSingleton(memory);

// Build .NET web app as usual
var app = appBuilder.Build();

// Read the settings, needed below
var config = app.Configuration.GetSection("SemanticMemory").Get<SemanticMemoryConfig>() ?? throw new ConfigurationException("Unable to load configuration");

// ********************************************************
// ************** WEB SERVICE ENDPOINTS *******************
// ********************************************************

#pragma warning disable CA2254 // The log msg template should be a constant
#pragma warning disable CA1031 // Catch all required to control all errors
// ReSharper disable once TemplateIsNotCompileTimeConstantProblem

if (config.Service.RunWebService)
{
    if (config.Service.OpenApiEnabled)
    {
        // URL: http://localhost:9001/swagger/index.html
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    DateTimeOffset start = DateTimeOffset.UtcNow;

    // Simple ping endpoint
    app.MapGet("/", () => Results.Ok("Ingestion service is running. " +
                                     "Uptime: " + (DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                                                   - start.ToUnixTimeSeconds()) + " secs " +
                                     $"- Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}"));
    // File upload endpoint
    app.MapPost(Constants.HttpUploadEndpoint, async Task<IResult> (
            HttpRequest request,
            ISemanticMemoryClient service,
            ILogger<Program> log) =>
        {
            log.LogTrace("New upload HTTP request");

            // Note: .NET doesn't yet support binding multipart forms including data and files
            (HttpDocumentUploadRequest input, bool isValid, string errMsg) = await HttpDocumentUploadRequest.BindHttpRequestAsync(request).ConfigureAwait(false);

            if (!isValid)
            {
                log.LogError(errMsg);
                return Results.BadRequest(errMsg);
            }

            try
            {
                // UploadRequest => Document
                var documentId = await service.ImportDocumentAsync(input.ToDocumentUploadRequest());
                var url = Constants.HttpUploadStatusEndpointWithParams
                    .Replace(Constants.HttpIndexPlaceholder, input.Index, StringComparison.Ordinal)
                    .Replace(Constants.HttpDocumentIdPlaceholder, documentId, StringComparison.Ordinal);
                return Results.Accepted(url, new UploadAccepted
                {
                    DocumentId = documentId,
                    Index = input.Index,
                    Message = "Document upload completed, ingestion pipeline started"
                });
            }
            catch (Exception e)
            {
                return Results.Problem(title: "Document upload failed", detail: e.Message, statusCode: 503);
            }
        })
        .Produces<UploadAccepted>(StatusCodes.Status202Accepted);

    // Delete document endpoint
    app.MapDelete(Constants.HttpDocumentsEndpoint,
            async Task<IResult> (
                [FromQuery(Name = Constants.WebServiceIndexField)]
                string? index,
                [FromQuery(Name = Constants.WebServiceDocumentIdField)]
                string documentId,
                ISemanticMemoryClient service,
                ILogger<Program> log) =>
            {
                log.LogTrace("New delete document HTTP request");
                await service.DeleteDocumentAsync(documentId: documentId, index: index);
                return Results.Accepted();
            })
        .Produces<MemoryAnswer>(StatusCodes.Status202Accepted);

    // Ask endpoint
    app.MapPost(Constants.HttpAskEndpoint,
            async Task<IResult> (
                MemoryQuery query,
                ISemanticMemoryClient service,
                ILogger<Program> log) =>
            {
                log.LogTrace("New search request");
                MemoryAnswer answer = await service.AskAsync(question: query.Question, index: query.Index, query.Filter);
                return Results.Ok(answer);
            })
        .Produces<MemoryAnswer>(StatusCodes.Status200OK);

    // Search endpoint
    app.MapPost(Constants.HttpSearchEndpoint,
            async Task<IResult> (
                SearchQuery query,
                ISemanticMemoryClient service,
                ILogger<Program> log) =>
            {
                log.LogTrace("New search HTTP request");
                SearchResult answer = await service.SearchAsync(query: query.Query, index: query.Index, query.Filter);
                return Results.Ok(answer);
            })
        .Produces<SearchResult>(StatusCodes.Status200OK);

    // Document status endpoint
    app.MapGet(Constants.HttpUploadStatusEndpoint,
            async Task<IResult> (
                [FromQuery(Name = Constants.WebServiceIndexField)]
                string? index,
                [FromQuery(Name = Constants.WebServiceDocumentIdField)]
                string documentId,
                ISemanticMemoryClient service,
                ILogger<Program> log) =>
            {
                log.LogTrace("New document status HTTP request");
                index = IndexExtensions.CleanName(index);

                if (string.IsNullOrEmpty(documentId))
                {
                    return Results.BadRequest($"'{Constants.WebServiceDocumentIdField}' query parameter is missing or has no value");
                }

                DataPipelineStatus? pipeline = await service.GetDocumentStatusAsync(documentId: documentId, index: index);
                if (pipeline == null)
                {
                    return Results.NotFound("Document not found");
                }

                return Results.Ok(pipeline);
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
    "Starting Semantic Memory service, .NET Env: {0}, Log Level: {1}, Web service: {2}, Pipeline handlers: {3}",
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
    app.Logger.GetLogLevelName(),
    config.Service.RunWebService,
    config.Service.RunHandlers);

app.Run();
