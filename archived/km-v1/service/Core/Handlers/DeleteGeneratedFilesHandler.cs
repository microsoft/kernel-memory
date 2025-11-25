// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.DocumentStorage;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.Handlers;

public sealed class DeleteGeneratedFilesHandler : IPipelineStepHandler
{
    private readonly IDocumentStorage _documentStorage;
    private readonly ILogger<DeleteGeneratedFilesHandler> _log;

    public string StepName { get; }

    public DeleteGeneratedFilesHandler(
        string stepName,
        IDocumentStorage documentStorage,
        ILoggerFactory? loggerFactory = null)
    {
        this.StepName = stepName;
        this._documentStorage = documentStorage;
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<DeleteGeneratedFilesHandler>();

        this._log.LogInformation("Handler '{0}' ready", stepName);
    }

    /// <inheritdoc />
    public async Task<(ReturnType returnType, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Deleting generated files, pipeline '{0}/{1}'", pipeline.Index, pipeline.DocumentId);

        // Delete files, leaving the status file
        await this._documentStorage.EmptyDocumentDirectoryAsync(
            index: pipeline.Index,
            documentId: pipeline.DocumentId,
            cancellationToken).ConfigureAwait(false);

        return (ReturnType.Success, pipeline);
    }
}
