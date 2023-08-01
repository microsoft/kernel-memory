// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Core.Pipeline;

namespace Microsoft.SemanticMemory.Core.WebService;

public static class UploadEndpoint
{
    public static async Task<IResult> UploadAsync(
        WebApplication app,
        HttpRequest request,
        IPipelineOrchestrator orchestrator,
        ILogger log)
    {
        log.LogTrace("New upload request");

        // Note: .NET doesn't yet support binding multipart forms including data and files
        (UploadRequest input, bool isValid, string errMsg) = await UploadRequest.BindHttpRequestAsync(request).ConfigureAwait(false);

        if (!isValid)
        {
#pragma warning disable CA2254 // The log msg template should be a constant
            // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
            log.LogError(errMsg);
#pragma warning restore CA2254
            return Results.BadRequest(errMsg);
        }

        log.LogInformation("Queueing upload of {0} files for further processing [request {1}]", input.Files.Count(), input.DocumentId);

        // Define all the steps in the pipeline
        var pipeline = orchestrator
            .PrepareNewFileUploadPipeline(
                documentId: input.DocumentId,
                userId: input.UserId, input.Tags, input.Files)
            .Then("extract")
            .Then("partition")
            .Then("gen_embeddings")
            .Then("save_embeddings")
            .Build();

        try
        {
            await orchestrator.RunPipelineAsync(pipeline).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Must catch all to log and keep the process alive
        catch (Exception e)
        {
            app.Logger.LogError(e, "Pipeline start failed");
            return Results.Problem(
                title: "Upload failed",
                detail: e.Message,
                statusCode: 503);
        }
#pragma warning restore CA1031

        return Results.Accepted($"/upload-status?id={pipeline.Id}", new
        {
            Id = pipeline.Id,
            Message = "Upload completed, pipeline started",
            Count = input.Files.Count()
        });
    }
}
