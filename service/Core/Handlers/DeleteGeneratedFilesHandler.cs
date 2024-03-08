// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.ContentStorage;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.Handlers;

public class DeleteGeneratedFilesHandler : IPipelineStepHandler
{
    private readonly KernelMemoryConfig _config;
    private readonly IContentStorage _contentStorage;
    private readonly ILogger<DeleteGeneratedFilesHandler> _log;

    public string StepName { get; }

    public DeleteGeneratedFilesHandler(
        string stepName,
        KernelMemoryConfig config,
        IContentStorage contentStorage,
        ILogger<DeleteGeneratedFilesHandler>? log = null)
    {
        this.StepName = stepName;
        this._config = config;
        this._contentStorage = contentStorage;
        this._log = log ?? DefaultLogger<DeleteGeneratedFilesHandler>.Instance;

        this._log.LogInformation("Handler '{0}' ready", stepName);
    }

    /// <inheritdoc />
    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        var index = IndexExtensions.CleanName(pipeline.Index, this._config.DefaultIndex);
        this._log.LogDebug("Deleting generated files, pipeline '{0}/{1}'", index, pipeline.DocumentId);

        // Delete files, leaving the status file
        await this._contentStorage.EmptyDocumentDirectoryAsync(
            index: index,
            documentId: pipeline.DocumentId,
            cancellationToken).ConfigureAwait(false);

        return (true, pipeline);
    }
}
