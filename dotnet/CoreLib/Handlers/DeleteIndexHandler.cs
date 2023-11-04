﻿// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.ContentStorage;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.Handlers;

public class DeleteIndexHandler : IPipelineStepHandler
{
    private readonly List<IVectorDb> _vectorDbs;
    private readonly IContentStorage _contentStorage;
    private readonly ILogger<DeleteIndexHandler> _log;

    public string StepName { get; }

    public DeleteIndexHandler(
        string stepName,
        IContentStorage contentStorage,
        List<IVectorDb> vectorDbs,
        ILogger<DeleteIndexHandler>? log = null)
    {
        this.StepName = stepName;
        this._contentStorage = contentStorage;
        this._vectorDbs = vectorDbs;
        this._log = log ?? DefaultLogger<DeleteIndexHandler>.Instance;

        this._log.LogInformation("Handler '{0}' ready", stepName);
    }

    /// <inheritdoc />
    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Deleting index, pipeline '{0}/{1}'", pipeline.Index, pipeline.DocumentId);

        // Delete index from vector storage
        foreach (IVectorDb db in this._vectorDbs)
        {
            await db.DeleteIndexAsync(indexName: pipeline.Index, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        // Delete index from file storage
        await this._contentStorage.DeleteIndexDirectoryAsync(
            index: pipeline.Index,
            cancellationToken).ConfigureAwait(false);

        return (true, pipeline);
    }
}
