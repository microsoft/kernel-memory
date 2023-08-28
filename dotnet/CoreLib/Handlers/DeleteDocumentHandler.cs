// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.ContentStorage;
using Microsoft.SemanticMemory.Diagnostics;
using Microsoft.SemanticMemory.MemoryStorage;
using Microsoft.SemanticMemory.Pipeline;

namespace Microsoft.SemanticMemory.Handlers;

public class DeleteDocumentHandler : IPipelineStepHandler
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly List<ISemanticMemoryVectorDb> _vectorDbs;
    private readonly IContentStorage _contentStorage;
    private readonly ILogger<DeleteDocumentHandler> _log;

    public string StepName { get; }

    public DeleteDocumentHandler(
        string stepName,
        IPipelineOrchestrator orchestrator,
        IContentStorage contentStorage,
        List<ISemanticMemoryVectorDb> vectorDbs,
        ILogger<DeleteDocumentHandler>? log = null)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;
        this._contentStorage = contentStorage;
        this._vectorDbs = vectorDbs;
        this._log = log ?? DefaultLogger<DeleteDocumentHandler>.Instance;

        this._log.LogInformation("Handler '{0}' ready", stepName);
    }

    /// <inheritdoc />
    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        // Delete embeddings
        foreach (ISemanticMemoryVectorDb db in this._vectorDbs)
        {
            IAsyncEnumerable<MemoryRecord> records = db.GetListAsync(
                indexName: pipeline.Index,
                limit: -1,
                filter: MemoryFilters.ByDocument(pipeline.DocumentId),
                cancellationToken: cancellationToken);

            await foreach (var record in records.WithCancellation(cancellationToken))
            {
                await db.DeleteAsync(indexName: pipeline.Index, record, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        // Delete files
        await this._contentStorage.DeleteDocumentDirectoryAsync(
            index: pipeline.Index,
            documentId: pipeline.DocumentId,
            cancellationToken).ConfigureAwait(false);

        return (true, pipeline);
    }
}
