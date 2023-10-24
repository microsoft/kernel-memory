// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.ContentStorage;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.Handlers;

public class DeleteDocumentHandler : IPipelineStepHandler
{
    private readonly List<IVectorDb> _vectorDbs;
    private readonly IContentStorage _contentStorage;
    private readonly ILogger<DeleteDocumentHandler> _log;

    public string StepName { get; }

    public DeleteDocumentHandler(
        string stepName,
        IContentStorage contentStorage,
        List<IVectorDb> vectorDbs,
        ILogger<DeleteDocumentHandler>? log = null)
    {
        this.StepName = stepName;
        this._contentStorage = contentStorage;
        this._vectorDbs = vectorDbs;
        this._log = log ?? DefaultLogger<DeleteDocumentHandler>.Instance;

        this._log.LogInformation("Handler '{0}' ready", stepName);
    }

    /// <inheritdoc />
    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Deleting document, pipeline '{0}/{1}'", pipeline.Index, pipeline.DocumentId);

        // Delete embeddings
        foreach (IVectorDb db in this._vectorDbs)
        {
            IAsyncEnumerable<MemoryRecord> records = db.GetListAsync(
                indexName: pipeline.Index,
                limit: -1,
                filters: new List<MemoryFilter> { MemoryFilters.ByDocument(pipeline.DocumentId) },
                cancellationToken: cancellationToken);

            await foreach (var record in records.WithCancellation(cancellationToken))
            {
                await db.DeleteAsync(indexName: pipeline.Index, record, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        // Delete files, leaving the status file
        await this._contentStorage.EmptyDocumentDirectoryAsync(
            index: pipeline.Index,
            documentId: pipeline.DocumentId,
            cancellationToken).ConfigureAwait(false);

        return (true, pipeline);
    }
}
