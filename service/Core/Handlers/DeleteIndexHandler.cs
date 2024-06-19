// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.DocumentStorage;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.Handlers;

public sealed class DeleteIndexHandler : IPipelineStepHandler
{
    private readonly List<IMemoryDb> _memoryDbs;
    private readonly IDocumentStorage _documentStorage;
    private readonly ILogger<DeleteIndexHandler> _log;

    public string StepName { get; }

    public DeleteIndexHandler(
        string stepName,
        IDocumentStorage documentStorage,
        List<IMemoryDb> memoryDbs,
        ILoggerFactory? loggerFactory = null)
    {
        this.StepName = stepName;
        this._documentStorage = documentStorage;
        this._memoryDbs = memoryDbs;
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<DeleteIndexHandler>();

        this._log.LogInformation("Handler '{0}' ready", stepName);
    }

    /// <inheritdoc />
    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Deleting index, pipeline '{0}/{1}'", pipeline.Index, pipeline.DocumentId);

        // Delete index from vector storage
        foreach (IMemoryDb db in this._memoryDbs)
        {
            await db.DeleteIndexAsync(index: pipeline.Index, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        // Delete index from file storage
        await this._documentStorage.DeleteIndexDirectoryAsync(
            index: pipeline.Index,
            cancellationToken).ConfigureAwait(false);

        return (true, pipeline);
    }
}
