// Copyright (c) Microsoft. All rights reserved.

// ReSharper disable RedundantUsingDirective

#pragma warning disable IDE0005 // temp

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Service.AspNetCore.Models;
using System.IO;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.KernelMemory.DocumentStorage;

namespace Microsoft.KernelMemory.Service.AspNetCore;

public static class WebAPIEndpoints
{
    public static IEndpointRouteBuilder AddKernelMemoryEndpoints(
        this IEndpointRouteBuilder builder,
        string apiPrefix = "/",
        IEndpointFilter? authFilter = null)
    {
        builder.AddPostUploadEndpoint(apiPrefix, authFilter);
        builder.AddGetIndexesEndpoint(apiPrefix, authFilter);
        builder.AddDeleteIndexesEndpoint(apiPrefix, authFilter);
        builder.AddDeleteDocumentsEndpoint(apiPrefix, authFilter);
        builder.AddAskEndpoint(apiPrefix, authFilter);
        builder.AddSearchEndpoint(apiPrefix, authFilter);
        builder.AddUploadStatusEndpoint(apiPrefix, authFilter);
        builder.AddGetDownloadEndpoint(apiPrefix, authFilter);

        return builder;
    }

    public static void AddPostUploadEndpoint(
        this IEndpointRouteBuilder builder, string apiPrefix = "/", IEndpointFilter? authFilter = null)
    {
        RouteGroupBuilder group = builder.MapGroup(apiPrefix);

        // File upload endpoint
        var route = group.MapPost(Constants.HttpUploadEndpoint, async Task<IResult> (
                HttpRequest request,
                IKernelMemory service,
                ILogger<KernelMemoryWebAPI> log,
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

    public static void AddGetIndexesEndpoint(
        this IEndpointRouteBuilder builder, string apiPrefix = "/", IEndpointFilter? authFilter = null)
    {
        RouteGroupBuilder group = builder.MapGroup(apiPrefix);

        // List of indexes endpoint
        var route = group.MapGet(Constants.HttpIndexesEndpoint,
                async Task<IResult> (
                    IKernelMemory service,
                    ILogger<KernelMemoryWebAPI> log,
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

    public static void AddDeleteIndexesEndpoint(
        this IEndpointRouteBuilder builder, string apiPrefix = "/", IEndpointFilter? authFilter = null)
    {
        RouteGroupBuilder group = builder.MapGroup(apiPrefix);

        // Delete index endpoint
        var route = group.MapDelete(Constants.HttpIndexesEndpoint,
                async Task<IResult> (
                    [FromQuery(Name = Constants.WebServiceIndexField)]
                    string? index,
                    IKernelMemory service,
                    ILogger<KernelMemoryWebAPI> log,
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

    public static void AddDeleteDocumentsEndpoint(
        this IEndpointRouteBuilder builder, string apiPrefix = "/", IEndpointFilter? authFilter = null)
    {
        RouteGroupBuilder group = builder.MapGroup(apiPrefix);

        // Delete document endpoint
        var route = group.MapDelete(Constants.HttpDocumentsEndpoint,
                async Task<IResult> (
                    [FromQuery(Name = Constants.WebServiceIndexField)]
                    string? index,
                    [FromQuery(Name = Constants.WebServiceDocumentIdField)]
                    string documentId,
                    IKernelMemory service,
                    ILogger<KernelMemoryWebAPI> log,
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

    public static void AddAskEndpoint(
        this IEndpointRouteBuilder builder, string apiPrefix = "/", IEndpointFilter? authFilter = null)
    {
        RouteGroupBuilder group = builder.MapGroup(apiPrefix);

        // Ask endpoint
        var route = group.MapPost(Constants.HttpAskEndpoint,
                async Task<IResult> (
                    MemoryQuery query,
                    IKernelMemory service,
                    ILogger<KernelMemoryWebAPI> log,
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

    public static void AddSearchEndpoint(
        this IEndpointRouteBuilder builder, string apiPrefix = "/", IEndpointFilter? authFilter = null)
    {
        RouteGroupBuilder group = builder.MapGroup(apiPrefix);

        // Search endpoint
        var route = group.MapPost(Constants.HttpSearchEndpoint,
                async Task<IResult> (
                    SearchQuery query,
                    IKernelMemory service,
                    ILogger<KernelMemoryWebAPI> log,
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

    public static void AddUploadStatusEndpoint(
        this IEndpointRouteBuilder builder, string apiPrefix = "/", IEndpointFilter? authFilter = null)
    {
        RouteGroupBuilder group = builder.MapGroup(apiPrefix);

        // Document status endpoint
        var route = group.MapGet(Constants.HttpUploadStatusEndpoint,
                async Task<IResult> (
                    [FromQuery(Name = Constants.WebServiceIndexField)]
                    string? index,
                    [FromQuery(Name = Constants.WebServiceDocumentIdField)]
                    string documentId,
                    IKernelMemory memoryClient,
                    ILogger<KernelMemoryWebAPI> log,
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

    public static void AddGetDownloadEndpoint(this IEndpointRouteBuilder builder, string apiPrefix = "/", IEndpointFilter? authFilter = null)
    {
        RouteGroupBuilder group = builder.MapGroup(apiPrefix);

        // File download endpoint
        var route = group.MapGet(Constants.HttpDownloadEndpoint, async Task<IResult> (
                [FromQuery(Name = Constants.WebServiceIndexField)]
                string? index,
                [FromQuery(Name = Constants.WebServiceDocumentIdField)]
                string documentId,
                [FromQuery(Name = Constants.WebServiceFilenameField)]
                string filename,
                HttpContext httpContext,
                IKernelMemory service,
                ILogger<KernelMemoryWebAPI> log,
                CancellationToken cancellationToken) =>
            {
                var isValid = !(
                    string.IsNullOrWhiteSpace(documentId) ||
                    string.IsNullOrWhiteSpace(filename));
                var errMsg = "Missing required parameter";

                log.LogTrace("New download file HTTP request, index {0}, documentId {1}, fileName {3}", index, documentId, filename);

                if (!isValid)
                {
                    log.LogError(errMsg);
                    return Results.Problem(detail: errMsg, statusCode: 400);
                }

                try
                {
                    // DownloadRequest => Document
                    var file = await service.ExportFileAsync(
                            documentId: documentId,
                            fileName: filename,
                            index: index,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    if (file == null)
                    {
                        log.LogWarning("Returned file is NULL, file not found");
                        return Results.Problem(title: "File not found", statusCode: 404);
                    }

                    log.LogTrace("Downloading file '{0}', size '{1}', type '{2}'", filename, file.FileSize, file.FileType);
                    Stream resultingFileStream = await file.GetStreamAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
                    var response = Results.Stream(
                        resultingFileStream,
                        contentType: file.FileType,
                        fileDownloadName: filename,
                        lastModified: file.LastWrite,
                        enableRangeProcessing: true);

                    // Add content length header if missing
                    if (response is FileStreamHttpResult { FileLength: null or 0 })
                    {
                        httpContext.Response.Headers.ContentLength = file.FileSize;
                    }

                    return response;
                }
                catch (DocumentStorageFileNotFoundException e)
                {
                    return Results.Problem(title: "File not found", detail: e.Message, statusCode: 404);
                }
                catch (Exception e)
                {
                    return Results.Problem(title: "File download failed", detail: e.Message, statusCode: 503);
                }
            })
            .Produces<StreamableFileContent>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        if (authFilter != null) { route.AddEndpointFilter(authFilter); }
    }

    // Class used to tag log entries and allow log filtering
    // ReSharper disable once ClassNeverInstantiated.Local
#pragma warning disable CA1812 // used by logger, can't be static
    private sealed class KernelMemoryWebAPI
    {
    }
#pragma warning restore CA1812
}
