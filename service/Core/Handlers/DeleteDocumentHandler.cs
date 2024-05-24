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

public sealed class DeleteDocumentHandler : IPipelineStepHandler
{
    private readonly List<IMemoryDb> _memoryDbs;
    private readonly IDocumentStorage _documentStorage;
    private readonly ILogger<DeleteDocumentHandler> _log;

    public string StepName { get; }

    public DeleteDocumentHandler(
        string stepName,
        IDocumentStorage documentStorage,
        List<IMemoryDb> memoryDbs,
        ILogger<DeleteDocumentHandler>? log = null)
    {
        this.StepName = stepName;
        this._documentStorage = documentStorage;
        this._memoryDbs = memoryDbs;
        this._log = log ?? DefaultLogger<DeleteDocumentHandler>.Instance;

        this._log.LogInformation("Handler '{0}' ready", stepName);
    }

    /// <inheritdoc />
    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Deleting document, pipeline '{0}/{1}'", pipeline.Index, pipeline.DocumentId);

        // Delete embeddings
        foreach (IMemoryDb db in this._memoryDbs)
        {
            IAsyncEnumerable<MemoryRecord> records = db.GetListAsync(
                index: pipeline.Index,
                limit: -1,
                filters: new List<MemoryFilter> { MemoryFilters.ByDocument(pipeline.DocumentId) },
                cancellationToken: cancellationToken);

            await foreach (var record in records.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                await db.DeleteAsync(index: pipeline.Index, record, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        // Delete files, leaving the status file
        await this._documentStorage.EmptyDocumentDirectoryAsync(
            index: pipeline.Index,
            documentId: pipeline.DocumentId,
            cancellationToken).ConfigureAwait(false);

        return (true, pipeline);
    }
}
