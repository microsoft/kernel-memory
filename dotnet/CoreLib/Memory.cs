// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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
///
/// TODO: pipeline structure is hardcoded, should allow custom handlers/steps
/// </summary>
public class Memory : ISemanticMemoryClient
{
    public Memory(IServiceProvider serviceProvider)
    {
        this._configuration = serviceProvider.GetService<SemanticMemoryConfig>()
                              ?? throw new SemanticMemoryException("Unable to load configuration. Are all the dependencies configured?");

        this._searchClient = serviceProvider.GetService<SearchClient>()
                             ?? throw new ConfigurationException("Unable to load search client. Are all the dependencies configured?");

        this._orchestrator = serviceProvider.GetService<InProcessPipelineOrchestrator>()
                             ?? throw new ConfigurationException("Unable to load orchestrator. Are all the dependencies configured?");
    }

    /// <inheritdoc />
    public Task<string> ImportDocumentAsync(DocumentUploadRequest uploadRequest, CancellationToken cancellationToken = default)
    {
        return this.ImportInternalAsync(uploadRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ImportDocumentAsync(Document document, CancellationToken cancellationToken = default)
    {
        return this.ImportInternalAsync(document, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ImportDocumentAsync(string fileName, DocumentDetails? details = null, CancellationToken cancellationToken = default)
    {
        return this.ImportInternalAsync(new Document(fileName) { Details = details ?? new DocumentDetails() }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsDocumentReadyAsync(string userId, string documentId, CancellationToken cancellationToken = default)
    {
        var orchestrator = await this.GetOrchestratorAsync(cancellationToken).ConfigureAwait(false);
        return await orchestrator.IsDocumentReadyAsync(userId, documentId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<DataPipelineStatus?> GetDocumentStatusAsync(string userId, string documentId, CancellationToken cancellationToken = default)
    {
        var orchestrator = await this.GetOrchestratorAsync(cancellationToken).ConfigureAwait(false);
        DataPipeline? pipeline = await orchestrator.ReadPipelineStatusAsync(userId, documentId, cancellationToken).ConfigureAwait(false);
        return pipeline?.ToDataPipelineStatus();
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

    private readonly SearchClient _searchClient;
    private readonly InProcessPipelineOrchestrator _orchestrator;
    private readonly SemanticMemoryConfig _configuration;
    private bool _orchestratorReady = false;

    // TODO: handle contentions
    private async Task<InProcessPipelineOrchestrator> GetOrchestratorAsync(CancellationToken cancellationToken)
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

    private async Task<string> ImportInternalAsync(Document document, CancellationToken cancellationToken)
    {
        DocumentUploadRequest uploadRequest = await document.ToDocumentUploadRequestAsync(cancellationToken).ConfigureAwait(false);
        InProcessPipelineOrchestrator orchestrator = await this.GetOrchestratorAsync(cancellationToken).ConfigureAwait(false);
        return await orchestrator.ImportDocumentAsync(uploadRequest, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> ImportInternalAsync(DocumentUploadRequest uploadRequest, CancellationToken cancellationToken)
    {
        InProcessPipelineOrchestrator orchestrator = await this.GetOrchestratorAsync(cancellationToken).ConfigureAwait(false);
        return await orchestrator.ImportDocumentAsync(uploadRequest, cancellationToken).ConfigureAwait(false);
    }

    #endregion
}
