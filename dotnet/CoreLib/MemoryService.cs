// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticMemory.Client;
using Microsoft.SemanticMemory.Client.Models;
using Microsoft.SemanticMemory.Core.Pipeline;
using Microsoft.SemanticMemory.Core.Search;
using Microsoft.SemanticMemory.Core.WebService;

namespace Microsoft.SemanticMemory.Core;

public class MemoryService : ISemanticMemoryClient
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly SearchClient _searchClient;

    public MemoryService(
        IPipelineOrchestrator orchestrator,
        SearchClient searchClient)
    {
        this._orchestrator = orchestrator;
        this._searchClient = searchClient;
    }

    /// <inheritdoc />
    public Task<string> ImportDocumentAsync(DocumentUploadRequest uploadRequest, CancellationToken cancellationToken = default)
    {
        return this._orchestrator.ImportDocumentAsync(uploadRequest, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> ImportDocumentAsync(Document document, CancellationToken cancellationToken = default)
    {
        return await this.ImportInternalAsync(document, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<string> ImportDocumentAsync(string fileName, DocumentDetails? details = null, CancellationToken cancellationToken = default)
    {
        return this.ImportInternalAsync(new Document(fileName) { Details = details ?? new DocumentDetails() }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> IsDocumentReadyAsync(string userId, string documentId, CancellationToken cancellationToken = default)
    {
        return this._orchestrator.IsDocumentReadyAsync(userId, documentId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<DataPipelineStatus?> GetDocumentStatusAsync(string userId, string documentId, CancellationToken cancellationToken = default)
    {
        return this._orchestrator.ReadPipelineSummaryAsync(userId, documentId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<SearchResult> SearchAsync(string query, MemoryFilter? filter = null, CancellationToken cancellationToken = default)
    {
        // TODO: the user ID might be in the filter
        return this.SearchAsync(new DocumentDetails().UserId, query, filter, cancellationToken);
    }

    /// <inheritdoc />
    public Task<SearchResult> SearchAsync(string userId, string query, MemoryFilter? filter = null, CancellationToken cancellationToken = default)
    {
        return this._searchClient.SearchAsync(userId, query, filter, cancellationToken);
    }

    /// <inheritdoc />
    public Task<MemoryAnswer> AskAsync(string question, MemoryFilter? filter = null, CancellationToken cancellationToken = default)
    {
        return this.AskAsync(new DocumentDetails().UserId, question, filter, cancellationToken);
    }

    /// <inheritdoc />
    public Task<MemoryAnswer> AskAsync(string userId, string question, MemoryFilter? filter = null, CancellationToken cancellationToken = default)
    {
        return this._searchClient.AskAsync(userId: userId, question: question, filter: filter, cancellationToken: cancellationToken);
    }

    #region private

    private async Task<string> ImportInternalAsync(Document document, CancellationToken cancellationToken)
    {
        DocumentUploadRequest uploadRequest = await document.ToDocumentUploadRequestAsync(cancellationToken).ConfigureAwait(false);
        return await this._orchestrator.ImportDocumentAsync(uploadRequest, cancellationToken).ConfigureAwait(false);
    }

    #endregion
}
