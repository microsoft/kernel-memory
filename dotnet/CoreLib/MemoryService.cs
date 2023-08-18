// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticMemory.Client;
using Microsoft.SemanticMemory.Client.Models;
using Microsoft.SemanticMemory.Core.Configuration;
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
        if (orchestrator == null)
        {
            throw new ConfigurationException("The orchestrator is NULL");
        }

        if (searchClient == null)
        {
            throw new ConfigurationException("The search client is NULL");
        }

        this._orchestrator = orchestrator;
        this._searchClient = searchClient;
    }

    /// <inheritdoc />
    public async Task<string> ImportDocumentAsync(Document document, string? index = null, CancellationToken cancellationToken = default)
    {
        DocumentUploadRequest uploadRequest = await document.ToDocumentUploadRequestAsync(index, cancellationToken).ConfigureAwait(false);
        return await this.ImportDocumentAsync(uploadRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> ImportDocumentAsync(string fileName, string? documentId = null, TagCollection? tags = null, string? index = null, CancellationToken cancellationToken = default)
    {
        var document = new Document(documentId, tags: tags).AddFile(fileName);
        var uploadRequest = await document.ToDocumentUploadRequestAsync(index, cancellationToken).ConfigureAwait(false);
        return await this.ImportDocumentAsync(uploadRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<string> ImportDocumentAsync(DocumentUploadRequest uploadRequest, CancellationToken cancellationToken = default)
    {
        var index = IndexExtensions.CleanName(uploadRequest.Index);
        return this._orchestrator.ImportDocumentAsync(index, uploadRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> IsDocumentReadyAsync(string documentId, string? index = null, CancellationToken cancellationToken = default)
    {
        index = IndexExtensions.CleanName(index);
        return this._orchestrator.IsDocumentReadyAsync(index: index, documentId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<DataPipelineStatus?> GetDocumentStatusAsync(string documentId, string? index = null, CancellationToken cancellationToken = default)
    {
        index = IndexExtensions.CleanName(index);
        return this._orchestrator.ReadPipelineSummaryAsync(index: index, documentId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<SearchResult> SearchAsync(string query, string? index = null, MemoryFilter? filter = null, CancellationToken cancellationToken = default)
    {
        index = IndexExtensions.CleanName(index);
        return this._searchClient.SearchAsync(index: index, query, filter, cancellationToken);
    }

    /// <inheritdoc />
    public Task<MemoryAnswer> AskAsync(string question, string? index = null, MemoryFilter? filter = null, CancellationToken cancellationToken = default)
    {
        index = IndexExtensions.CleanName(index);
        return this._searchClient.AskAsync(index: index, question: question, filter: filter, cancellationToken: cancellationToken);
    }
}
