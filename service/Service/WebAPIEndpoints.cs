// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
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

        app.UseGetStatusEndpoint(authFilter);
        app.UsePostUploadEndpoint(authFilter);
        app.UseGetIndexesEndpoint(authFilter);
        app.UseDeleteIndexesEndpoint(authFilter);
        app.UseDeleteDocumentsEndpoint(authFilter);
        app.UseAskEndpoint(authFilter);
        app.UseSearchEndpoint(authFilter);
        app.UseUploadStatusEndpoint(authFilter);
    }

    public static void UseGetStatusEndpoint(this IEndpointRouteBuilder app, IEndpointFilter? authFilter = null)
    {
        // Simple ping endpoint
        var route = app.MapGet("/", () => Results.Ok("Ingestion service is running. " +
                                                     "Uptime: " + (DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                                                                   - s_start.ToUnixTimeSeconds()) + " secs " +
                                                     $"- Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}"))
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        if (authFilter != null) { route.AddEndpointFilter(authFilter); }
    }

    public static void UsePostUploadEndpoint(this IEndpointRouteBuilder app, IEndpointFilter? authFilter = null)
    {
        // File upload endpoint
        var route = app.MapPost(Constants.HttpUploadEndpoint, async Task<IResult> (
                HttpRequest request,
                IKernelMemory service,
                ILogger<WebAPIEndpoint> log,
                CancellationToken cancellationToken) =>
            {
                log.LogTrace("New upload HTTP request, content length {0}", request.ContentLength);

                // Note: .NET doesn't yet support binding multipart forms including data and files
                (HttpDocumentUploadRequest input, bool isValid, string errMsg)
                    = await HttpDocumentUploadRequest.BindHttpRequestAsync(request, cancellationToken)
                        .ConfigureAwait(false);

                log.LogTrace("Index '{0}'", input.Index);

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

                    log.LogTrace("Doc Id '{1}'", documentId);

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
            .Produces<UploadAccepted>(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        if (authFilter != null) { route.AddEndpointFilter(authFilter); }
    }

    public static void UseGetIndexesEndpoint(this IEndpointRouteBuilder app, IEndpointFilter? authFilter = null)
    {
        // List of indexes endpoint
        var route = app.MapGet(Constants.HttpIndexesEndpoint,
                async Task<IResult> (
                    IKernelMemory service,
                    ILogger<WebAPIEndpoint> log,
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
            .Produces<IndexCollection>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        if (authFilter != null) { route.AddEndpointFilter(authFilter); }
    }

    public static void UseDeleteIndexesEndpoint(this IEndpointRouteBuilder app, IEndpointFilter? authFilter = null)
    {
        // Delete index endpoint
        var route = app.MapDelete(Constants.HttpIndexesEndpoint,
                async Task<IResult> (
                    [FromQuery(Name = Constants.WebServiceIndexField)]
                    string? index,
                    IKernelMemory service,
                    ILogger<WebAPIEndpoint> log,
                    CancellationToken cancellationToken) =>
                {
                    log.LogTrace("New delete document HTTP request, index '{0}'", index);
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
            .Produces<DeleteAccepted>(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        if (authFilter != null) { route.AddEndpointFilter(authFilter); }
    }

    public static void UseDeleteDocumentsEndpoint(this IEndpointRouteBuilder app, IEndpointFilter? authFilter = null)
    {
        // Delete document endpoint
        var route = app.MapDelete(Constants.HttpDocumentsEndpoint,
                async Task<IResult> (
                    [FromQuery(Name = Constants.WebServiceIndexField)]
                    string? index,
                    [FromQuery(Name = Constants.WebServiceDocumentIdField)]
                    string documentId,
                    IKernelMemory service,
                    ILogger<WebAPIEndpoint> log,
                    CancellationToken cancellationToken) =>
                {
                    log.LogTrace("New delete document HTTP request, index '{0}'", index);
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
            .Produces<DeleteAccepted>(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        if (authFilter != null) { route.AddEndpointFilter(authFilter); }
    }

    public static void UseAskEndpoint(this IEndpointRouteBuilder app, IEndpointFilter? authFilter = null)
    {
        // Ask endpoint
        var route = app.MapPost(Constants.HttpAskEndpoint,
                async Task<IResult> (
                    MemoryQuery query,
                    IKernelMemory service,
                    ILogger<WebAPIEndpoint> log,
                    CancellationToken cancellationToken) =>
                {
                    log.LogTrace("New search request, index '{0}', minRelevance {1}", query.Index, query.MinRelevance);
                    MemoryAnswer answer = await service.AskAsync(
                            question: query.Question,
                            index: query.Index,
                            filters: query.Filters,
                            minRelevance: query.MinRelevance,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    return Results.Ok(answer);
                })
            .Produces<MemoryAnswer>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        if (authFilter != null) { route.AddEndpointFilter(authFilter); }
    }

    public static void UseSearchEndpoint(this IEndpointRouteBuilder app, IEndpointFilter? authFilter = null)
    {
        // Search endpoint
        var route = app.MapPost(Constants.HttpSearchEndpoint,
                async Task<IResult> (
                    SearchQuery query,
                    IKernelMemory service,
                    ILogger<WebAPIEndpoint> log,
                    CancellationToken cancellationToken) =>
                {
                    log.LogTrace("New search HTTP request, index '{0}', minRelevance {1}", query.Index, query.MinRelevance);
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
            .Produces<SearchResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        if (authFilter != null) { route.AddEndpointFilter(authFilter); }
    }

    public static void UseUploadStatusEndpoint(this IEndpointRouteBuilder app, IEndpointFilter? authFilter = null)
    {
        // Document status endpoint
        var route = app.MapGet(Constants.HttpUploadStatusEndpoint,
                async Task<IResult> (
                    [FromQuery(Name = Constants.WebServiceIndexField)]
                    string? index,
                    [FromQuery(Name = Constants.WebServiceDocumentIdField)]
                    string documentId,
                    IKernelMemory memoryClient,
                    ILogger<WebAPIEndpoint> log,
                    CancellationToken cancellationToken) =>
                {
                    log.LogTrace("New document status HTTP request");
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
            .Produces<DataPipelineStatus>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        if (authFilter != null) { route.AddEndpointFilter(authFilter); }
    }

    // Class used to tag log entries and allow log filtering
    // ReSharper disable once ClassNeverInstantiated.Local
#pragma warning disable CA1812 // used by logger, can't be static
    private sealed class WebAPIEndpoint
    {
    }
#pragma warning restore CA1812
}
