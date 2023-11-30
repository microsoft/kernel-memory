// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.ContentStorage;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.Handlers;

public class DeleteDocumentGeneratedFiles : IPipelineStepHandler
{
    private readonly IContentStorage _contentStorage;
    private readonly ILogger<DeleteDocumentGeneratedFiles> _log;

    public string StepName { get; }

    public DeleteDocumentGeneratedFiles(
        string stepName,
        IContentStorage contentStorage,
        ILogger<DeleteDocumentGeneratedFiles>? log = null)
    {
        this.StepName = stepName;
        this._contentStorage = contentStorage;
        this._log = log ?? DefaultLogger<DeleteDocumentGeneratedFiles>.Instance;

        this._log.LogInformation("Handler '{0}' ready", stepName);
    }

    /// <inheritdoc />
    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Deleting document generated files, pipeline '{0}/{1}'", pipeline.Index, pipeline.DocumentId);

        // Delete files, leaving the status file
        await this._contentStorage.EmptyDocumentDirectoryAsync(
            index: pipeline.Index,
            documentId: pipeline.DocumentId,
            cancellationToken).ConfigureAwait(false);

        return (true, pipeline);
    }
}
