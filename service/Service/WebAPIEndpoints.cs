// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.WebService;

namespace Microsoft.KernelMemory.Service;

internal static class WebAPIEndpoints
{
    private static readonly DateTimeOffset s_start = DateTimeOffset.UtcNow;

    public static void ConfigureMinimalAPI(this WebApplication app, KernelMemoryConfig config)
    {
        if (!config.Service.RunWebService) { return; }

        app.UseSwagger(config);

        var authFilter = new HttpAuthEndpointFilter(config.ServiceAuthorization);

        app.UseGetStatusEndpoint(config, authFilter);
        app.UsePostUploadEndpoint(config, authFilter);
        app.UseGetIndexesEndpoint(config, authFilter);
        app.UseDeleteIndexesEndpoint(config, authFilter);
        app.UseDeleteDocumentsEndpoint(config, authFilter);
        app.UseAskEndpoint(config, authFilter);
        app.UseSearchEndpoint(config, authFilter);
        app.UseUploadStatusEndpoint(config, authFilter);
    }

    public static void UseGetStatusEndpoint(this WebApplication app, KernelMemoryConfig config, HttpAuthEndpointFilter authFilter)
    {
        if (!config.Service.RunWebService) { return; }

        // Simple ping endpoint
        app.MapGet("/", () => Results.Ok("Ingestion service is running. " +
                                         "Uptime: " + (DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                                                       - s_start.ToUnixTimeSeconds()) + " secs " +
                                         $"- Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}"))
            .AddEndpointFilter(authFilter)
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);
    }

    public static void UsePostUploadEndpoint(this WebApplication app, KernelMemoryConfig config, HttpAuthEndpointFilter authFilter)
    {
        if (!config.Service.RunWebService) { return; }

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
    }

    public static void UseGetIndexesEndpoint(this WebApplication app, KernelMemoryConfig config, HttpAuthEndpointFilter authFilter)
    {
        if (!config.Service.RunWebService) { return; }

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
    }

    public static void UseDeleteIndexesEndpoint(this WebApplication app, KernelMemoryConfig config, HttpAuthEndpointFilter authFilter)
    {
        if (!config.Service.RunWebService) { return; }

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
    }

    public static void UseDeleteDocumentsEndpoint(this WebApplication app, KernelMemoryConfig config, HttpAuthEndpointFilter authFilter)
    {
        if (!config.Service.RunWebService) { return; }

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
    }

    public static void UseAskEndpoint(this WebApplication app, KernelMemoryConfig config, HttpAuthEndpointFilter authFilter)
    {
        if (!config.Service.RunWebService) { return; }

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
    }

    public static void UseSearchEndpoint(this WebApplication app, KernelMemoryConfig config, HttpAuthEndpointFilter authFilter)
    {
        if (!config.Service.RunWebService) { return; }

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
    }

    public static void UseUploadStatusEndpoint(this WebApplication app, KernelMemoryConfig config, HttpAuthEndpointFilter authFilter)
    {
        if (!config.Service.RunWebService) { return; }

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
}
