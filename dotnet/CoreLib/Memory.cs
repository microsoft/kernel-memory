// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticMemory.Client;
using Microsoft.SemanticMemory.Client.Models;
using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.Handlers;
using Microsoft.SemanticMemory.Core.Pipeline;
using Microsoft.SemanticMemory.Core.Search;
using Microsoft.SemanticMemory.Core.WebService;

namespace Microsoft.SemanticMemory.Core;

/// <summary>
/// Memory client to upload files and search for answers, without depending
/// on a web service. By design this class is hardcoded to use
/// <see cref="InProcessPipelineOrchestrator"/>, hence the name "Serverless".
/// The class accesses directly storage, vectors and AI.
/// </summary>
public class Memory : ISemanticMemoryClient
{
    public Memory(
        InProcessPipelineOrchestrator orchestrator,
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
    public async Task<string> ImportDocumentAsync(DocumentUploadRequest uploadRequest, CancellationToken cancellationToken = default)
    {
        var index = IndexExtensions.CleanName(uploadRequest.Index);
        var orchestrator = await this.GetOrchestratorAsync(cancellationToken).ConfigureAwait(false);
        return await orchestrator.ImportDocumentAsync(index, uploadRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> IsDocumentReadyAsync(string documentId, string? index = null, CancellationToken cancellationToken = default)
    {
        index = IndexExtensions.CleanName(index);
        var orchestrator = await this.GetOrchestratorAsync(cancellationToken).ConfigureAwait(false);
        return await orchestrator.IsDocumentReadyAsync(index: index, documentId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<DataPipelineStatus?> GetDocumentStatusAsync(string documentId, string? index = null, CancellationToken cancellationToken = default)
    {
        index = IndexExtensions.CleanName(index);
        var orchestrator = await this.GetOrchestratorAsync(cancellationToken).ConfigureAwait(false);
        DataPipeline? pipeline = await orchestrator.ReadPipelineStatusAsync(index: index, documentId, cancellationToken).ConfigureAwait(false);
        return pipeline?.ToDataPipelineStatus();
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

    #region private

    private readonly SearchClient _searchClient;
    private readonly InProcessPipelineOrchestrator _orchestrator;
    private bool _orchestratorReady = false;

    // TODO: handle contentions
    // TODO: allow custom handlers, remove hardcoded ones
    private async Task<IPipelineOrchestrator> GetOrchestratorAsync(CancellationToken cancellationToken)
    {
        if (!this._orchestratorReady)
        {
            // Text extraction handler
            TextExtractionHandler textExtraction = new("extract", this._orchestrator);
            await this._orchestrator.AddHandlerAsync(textExtraction, cancellationToken).ConfigureAwait(false);

            // Text partitioning handler
            TextPartitioningHandler textPartitioning = new("partition", this._orchestrator);
            await this._orchestrator.AddHandlerAsync(textPartitioning, cancellationToken).ConfigureAwait(false);

            // Embedding generation handler
            GenerateEmbeddingsHandler textEmbedding = new("gen_embeddings", this._orchestrator);
            await this._orchestrator.AddHandlerAsync(textEmbedding, cancellationToken).ConfigureAwait(false);

            // Embedding storage handler
            SaveEmbeddingsHandler saveEmbedding = new("save_embeddings", this._orchestrator);
            await this._orchestrator.AddHandlerAsync(saveEmbedding, cancellationToken).ConfigureAwait(false);

            this._orchestratorReady = true;
        }

        return this._orchestrator;
    }

    #endregion
}
