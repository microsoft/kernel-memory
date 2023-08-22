// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticMemory.Configuration;
using Microsoft.SemanticMemory.Handlers;
using Microsoft.SemanticMemory.Pipeline;
using Microsoft.SemanticMemory.Search;
using Microsoft.SemanticMemory.WebService;

// ReSharper disable once CheckNamespace
namespace Microsoft.SemanticMemory;

/// <summary>
/// Memory client to upload files and search for answers, without depending
/// on a web service. By design this class is hardcoded to use
/// <see cref="InProcessPipelineOrchestrator"/>, hence the name "Serverless".
/// The class accesses directly storage, vectors and AI.
/// </summary>
public class Memory : ISemanticMemoryClient
{
    private readonly SearchClient _searchClient;
    private readonly InProcessPipelineOrchestrator _orchestrator;

    public InProcessPipelineOrchestrator Orchestrator => this._orchestrator;

    public Memory(
        InProcessPipelineOrchestrator orchestrator,
        SearchClient searchClient)
    {
        this._orchestrator = orchestrator ?? throw new ConfigurationException("The orchestrator is NULL");
        this._searchClient = searchClient ?? throw new ConfigurationException("The search client is NULL");

        // Default handlers - Use AddHandler to replace them.
        this.AddHandler(new TextExtractionHandler("extract", this._orchestrator));
        this.AddHandler(new TextPartitioningHandler("partition", this._orchestrator));
        this.AddHandler(new SummarizationHandler("summarize", this._orchestrator));
        this.AddHandler(new GenerateEmbeddingsHandler("gen_embeddings", this._orchestrator));
        this.AddHandler(new SaveEmbeddingsHandler("save_embeddings", this._orchestrator));
    }

    /// <summary>
    /// Register a pipeline handler. If a handler for the same step name already exists, it gets replaced.
    /// </summary>
    /// <param name="handler">Handler instance</param>
    public void AddHandler(IPipelineStepHandler handler)
    {
        this._orchestrator.AddHandler(handler);
    }

    /// <inheritdoc />
    public async Task<string> ImportDocumentAsync(
        Document document,
        string? index = null,
        IEnumerable<string>? steps = null,
        CancellationToken cancellationToken = default)
    {
        DocumentUploadRequest uploadRequest = await document.ToDocumentUploadRequestAsync(index, steps, cancellationToken).ConfigureAwait(false);
        return await this.ImportDocumentAsync(uploadRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> ImportDocumentAsync(
        string fileName,
        string? documentId = null,
        TagCollection? tags = null,
        string? index = null,
        IEnumerable<string>? steps = null,
        CancellationToken cancellationToken = default)
    {
        var document = new Document(documentId, tags: tags).AddFile(fileName);
        var uploadRequest = await document.ToDocumentUploadRequestAsync(index, steps, cancellationToken).ConfigureAwait(false);
        return await this.ImportDocumentAsync(uploadRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> ImportDocumentAsync(
        DocumentUploadRequest uploadRequest,
        CancellationToken cancellationToken = default)
    {
        var index = IndexExtensions.CleanName(uploadRequest.Index);
        return await this._orchestrator.ImportDocumentAsync(index, uploadRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> IsDocumentReadyAsync(
        string documentId,
        string? index = null,
        CancellationToken cancellationToken = default)
    {
        index = IndexExtensions.CleanName(index);
        return await this._orchestrator.IsDocumentReadyAsync(index: index, documentId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<DataPipelineStatus?> GetDocumentStatusAsync(
        string documentId,
        string? index = null,
        CancellationToken cancellationToken = default)
    {
        index = IndexExtensions.CleanName(index);
        DataPipeline? pipeline = await this._orchestrator.ReadPipelineStatusAsync(index: index, documentId, cancellationToken).ConfigureAwait(false);
        return pipeline?.ToDataPipelineStatus();
    }

    /// <inheritdoc />
    public Task<SearchResult> SearchAsync(
        string query,
        string? index = null,
        MemoryFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        index = IndexExtensions.CleanName(index);
        return this._searchClient.SearchAsync(index: index, query, filter, cancellationToken);
    }

    /// <inheritdoc />
    public Task<MemoryAnswer> AskAsync(
        string question,
        string? index = null,
        MemoryFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        index = IndexExtensions.CleanName(index);
        return this._searchClient.AskAsync(index: index, question: question, filter: filter, cancellationToken: cancellationToken);
    }
}
