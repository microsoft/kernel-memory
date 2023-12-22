// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.ContentStorage;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.InteractiveSetup;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.Service;
using Microsoft.KernelMemory.WebService;
using Microsoft.OpenApi.Models;

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

// Read the settings, needed below
var config = appBuilder.Configuration.GetSection("KernelMemory").Get<KernelMemoryConfig>()
             ?? throw new ConfigurationException("Unable to load configuration");
config.ServiceAuthorization.Validate();

CheckConfiguration();

// OpenAPI/swagger
if (config.Service.RunWebService)
{
    appBuilder.Services.AddEndpointsApiExplorer();
    appBuilder.Services.AddSwaggerGen(c =>
    {
        if (!config.ServiceAuthorization.Enabled) { return; }

        const string ReqName = "auth";
        c.AddSecurityDefinition(ReqName, new OpenApiSecurityScheme
        {
            Description = "The API key to access the API",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "ApiKeyScheme",
            Name = config.ServiceAuthorization.HttpHeaderName,
            In = ParameterLocation.Header,
        });

        var scheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Id = ReqName,
                Type = ReferenceType.SecurityScheme,
            },
            In = ParameterLocation.Header
        };

        var requirement = new OpenApiSecurityRequirement
        {
            { scheme, new List<string>() }
        };

        c.AddSecurityRequirement(requirement);
    });
}

// Inject memory client and its dependencies
// Note: pass the current service collection to the builder, in order to start the pipeline handlers
IKernelMemory memory = new KernelMemoryBuilder(appBuilder.Services)
    .FromAppSettings()
    // .With...() // in case you need to set something not already defined by `.FromAppSettings()`
    .Build();

appBuilder.Services.AddSingleton(memory);

// Build .NET web app as usual
var app = appBuilder.Build();

Console.WriteLine("***************************************************************************************************************************");
Console.WriteLine($"* Web service         : " + (config.Service.RunWebService ? "Enabled" : "Disabled"));
Console.WriteLine($"* Web service auth    : " + (config.ServiceAuthorization.Enabled ? "Enabled" : "Disabled"));
Console.WriteLine($"* Pipeline handlers   : " + (config.Service.RunHandlers ? "Enabled" : "Disabled"));
Console.WriteLine($"* OpenAPI swagger     : " + (config.Service.OpenApiEnabled ? "Enabled" : "Disabled"));
Console.WriteLine($"* Logging level       : {app.Logger.GetLogLevelName()}");
Console.WriteLine($"* Memory Db           : {app.Services.GetService<IMemoryDb>()?.GetType().FullName}");
Console.WriteLine($"* Content storage     : {app.Services.GetService<IContentStorage>()?.GetType().FullName}");
Console.WriteLine($"* Embedding generation: {app.Services.GetService<ITextEmbeddingGenerator>()?.GetType().FullName}");
Console.WriteLine($"* Text generation     : {app.Services.GetService<ITextGenerator>()?.GetType().FullName}");
Console.WriteLine("***************************************************************************************************************************");

// ********************************************************
// ************** WEB SERVICE ENDPOINTS *******************
// ********************************************************

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
    var authFilter = new HttpAuthEndpointFilter(config.ServiceAuthorization);

    // Simple ping endpoint
    app.MapGet("/", () => Results.Ok("Ingestion service is running. " +
                                     "Uptime: " + (DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                                                   - start.ToUnixTimeSeconds()) + " secs " +
                                     $"- Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}"))
        .AddEndpointFilter(authFilter)
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    // File upload endpoint
    app.MapPost(Constants.HttpUploadEndpoint, async Task<IResult> (
            HttpRequest request,
            IKernelMemory service,
            ILogger<Program> log,
            CancellationToken cancellationToken) =>
        {
            log.LogTrace("New upload HTTP request");

            // Note: .NET doesn't yet support binding multipart forms including data and files
            (HttpDocumentUploadRequest input, bool isValid, string errMsg)
                = await HttpDocumentUploadRequest.BindHttpRequestAsync(request, cancellationToken)
                    .ConfigureAwait(false);

            if (!isValid)
            {
                log.LogError(errMsg);
                return Results.Problem(detail: errMsg, statusCode: 400);
            }

            try
            {
                // UploadRequest => Document
                var documentId = await service.ImportDocumentAsync(input.ToDocumentUploadRequest(), cancellationToken)
                    .ConfigureAwait(false);
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
        .AddEndpointFilter(authFilter)
        .Produces<UploadAccepted>(StatusCodes.Status202Accepted)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
        .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

    // List of indexes endpoint
    app.MapGet(Constants.HttpIndexesEndpoint,
            async Task<IResult> (
                IKernelMemory service,
                ILogger<Program> log,
                CancellationToken cancellationToken) =>
            {
                log.LogTrace("New index list HTTP request");

                var result = new IndexCollection();
                IEnumerable<IndexDetails> list = await service.ListIndexesAsync(cancellationToken)
                    .ConfigureAwait(false);

                foreach (IndexDetails index in list)
                {
                    result.Results.Add(index);
                }

                return Results.Ok(result);
            })
        .AddEndpointFilter(authFilter)
        .Produces<IndexCollection>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    // Delete index endpoint
    app.MapDelete(Constants.HttpIndexesEndpoint,
            async Task<IResult> (
                [FromQuery(Name = Constants.WebServiceIndexField)]
                string? index,
                IKernelMemory service,
                ILogger<Program> log,
                CancellationToken cancellationToken) =>
            {
                log.LogTrace("New delete document HTTP request");
                await service.DeleteIndexAsync(index: index, cancellationToken)
                    .ConfigureAwait(false);
                // There's no API to check the index deletion progress, so the URL is empty
                var url = string.Empty;
                return Results.Accepted(url, new DeleteAccepted
                {
                    Index = index ?? string.Empty,
                    Message = "Index deletion request received, pipeline started"
                });
            })
        .AddEndpointFilter(authFilter)
        .Produces<DeleteAccepted>(StatusCodes.Status202Accepted)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    // Delete document endpoint
    app.MapDelete(Constants.HttpDocumentsEndpoint,
            async Task<IResult> (
                [FromQuery(Name = Constants.WebServiceIndexField)]
                string? index,
                [FromQuery(Name = Constants.WebServiceDocumentIdField)]
                string documentId,
                IKernelMemory service,
                ILogger<Program> log,
                CancellationToken cancellationToken) =>
            {
                log.LogTrace("New delete document HTTP request");
                await service.DeleteDocumentAsync(documentId: documentId, index: index, cancellationToken)
                    .ConfigureAwait(false);
                var url = Constants.HttpUploadStatusEndpointWithParams
                    .Replace(Constants.HttpIndexPlaceholder, index, StringComparison.Ordinal)
                    .Replace(Constants.HttpDocumentIdPlaceholder, documentId, StringComparison.Ordinal);
                return Results.Accepted(url, new DeleteAccepted
                {
                    DocumentId = documentId,
                    Index = index ?? string.Empty,
                    Message = "Document deletion request received, pipeline started"
                });
            })
        .AddEndpointFilter(authFilter)
        .Produces<DeleteAccepted>(StatusCodes.Status202Accepted)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    // Ask endpoint
    app.MapPost(Constants.HttpAskEndpoint,
            async Task<IResult> (
                MemoryQuery query,
                IKernelMemory service,
                ILogger<Program> log,
                CancellationToken cancellationToken) =>
            {
                log.LogTrace("New search request");
                MemoryAnswer answer = await service.AskAsync(
                        question: query.Question,
                        index: query.Index,
                        filters: query.Filters,
                        minRelevance: query.MinRelevance,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(answer);
            })
        .AddEndpointFilter(authFilter)
        .Produces<MemoryAnswer>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    // Search endpoint
    app.MapPost(Constants.HttpSearchEndpoint,
            async Task<IResult> (
                SearchQuery query,
                IKernelMemory service,
                ILogger<Program> log,
                CancellationToken cancellationToken) =>
            {
                log.LogTrace("New search HTTP request");
                SearchResult answer = await service.SearchAsync(
                        query: query.Query,
                        index: query.Index,
                        filters: query.Filters,
                        minRelevance: query.MinRelevance,
                        limit: query.Limit,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(answer);
            })
        .AddEndpointFilter(authFilter)
        .Produces<SearchResult>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    // Document status endpoint
    app.MapGet(Constants.HttpUploadStatusEndpoint,
            async Task<IResult> (
                [FromQuery(Name = Constants.WebServiceIndexField)]
                string? index,
                [FromQuery(Name = Constants.WebServiceDocumentIdField)]
                string documentId,
                IKernelMemory memoryClient,
                ILogger<Program> log,
                CancellationToken cancellationToken) =>
            {
                log.LogTrace("New document status HTTP request");
                index = IndexExtensions.CleanName(index);

                if (string.IsNullOrEmpty(documentId))
                {
                    return Results.Problem(detail: $"'{Constants.WebServiceDocumentIdField}' query parameter is missing or has no value", statusCode: 400);
                }

                DataPipelineStatus? pipeline = await memoryClient.GetDocumentStatusAsync(documentId: documentId, index: index, cancellationToken)
                    .ConfigureAwait(false);
                if (pipeline == null)
                {
                    return Results.Problem(detail: "Document not found", statusCode: 404);
                }

                if (pipeline.Empty)
                {
                    return Results.Problem(detail: "Empty pipeline", statusCode: 404);
                }

                return Results.Ok(pipeline);
            })
        .AddEndpointFilter(authFilter)
        .Produces<DataPipelineStatus>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
}
#pragma warning restore CA1031
#pragma warning restore CA2254

// ********************************************************
// ************** START ***********************************
// ********************************************************

app.Logger.LogInformation(
    "Starting Kernel Memory service, .NET Env: {0}, Log Level: {1}, Web service: {2}, Auth: {3}, Pipeline handlers: {4}",
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
    app.Logger.GetLogLevelName(),
    config.Service.RunWebService,
    config.ServiceAuthorization.Enabled,
    config.Service.RunHandlers);

app.Run();

void CheckConfiguration()
{
    const string Help = """
                        You can set your configuration in appsettings.json or appsettings.<current environment>.json.
                        The value of <current environment> depends on ASPNETCORE_ENVIRONMENT environment variable, and
                        is usually either "Development" or "Production".

                        You can also run `dotnet run setup` to launch a wizard that will guide through the creation
                        of a basic working version of "appsettings.Development.json".

                        If you would like to setup the service to use custom dependencies, e.g. a custom storage or
                        a custom LLM, you should edit Program.cs accordingly, setting up your dependencies with the
                        usual .NET dependency injection approach.
                        """;

    if (config.DataIngestion.EmbeddingGenerationEnabled && config.DataIngestion.EmbeddingGeneratorTypes.Count == 0)
    {
        Console.WriteLine("\n******\nData ingestion embedding generation (DataIngestion.EmbeddingGeneratorTypes) is not configured.\n" +
                          $"Please configure the service and retry.\n\n{Help}\n******\n");
        Environment.Exit(-1);
    }

    if (string.IsNullOrEmpty(config.TextGeneratorType))
    {
        Console.WriteLine("\n******\nText generation (TextGeneratorType) is not configured.\n" +
                          $"Please configure the service and retry.\n\n{Help}\n******\n");
    }

    if (string.IsNullOrEmpty(config.Retrieval.EmbeddingGeneratorType))
    {
        Console.WriteLine("\n******\nRetrieval embedding generation (Retrieval.EmbeddingGeneratorType) is not configured.\n" +
                          $"Please configure the service and retry.\n\n{Help}\n******\n");
        Environment.Exit(-1);
    }
}
