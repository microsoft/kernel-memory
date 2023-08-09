// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.Client;
using Microsoft.SemanticMemory.Client.Models;
using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.Handlers;
using Microsoft.SemanticMemory.Core.Pipeline;
using Microsoft.SemanticMemory.Core.Search;

namespace Microsoft.SemanticMemory.Core;

/// <summary>
/// Memory client to upload files and search for answers, without depending
/// on a web service. By design this class is hardcoded to use
/// <see cref="InProcessPipelineOrchestrator"/>, hence the name "Serverless".
/// The class accesses directly storage, vectors and AI.
///
/// TODO: pipeline structure is hardcoded, should allow custom handlers/steps
/// </summary>
public class SemanticMemoryServerless : ISemanticMemoryClient
{
    public SemanticMemoryServerless(IServiceProvider serviceProvider)
    {
        this._configuration = serviceProvider.GetService<SemanticMemoryConfig>()
                              ?? throw new SemanticMemoryException("Unable to load configuration. Are all the dependencies configured?");

        this._searchClient = serviceProvider.GetService<SearchClient>()
                             ?? throw new ConfigurationException("Unable to load search client. Are all the dependencies configured?");

        this._orchestrator = serviceProvider.GetService<InProcessPipelineOrchestrator>()
                             ?? throw new ConfigurationException("Unable to load orchestrator. Are all the dependencies configured?");
    }

    /// <inheritdoc />
    public async Task<string> ImportFileAsync(Document file, CancellationToken cancellationToken = default)
    {
        var ids = await this.ImportFilesAsync(new[] { file }, cancellationToken).ConfigureAwait(false);
        return ids.First();
    }

    /// <inheritdoc />
    public Task<IList<string>> ImportFilesAsync(Document[] files, CancellationToken cancellationToken = default)
    {
        return this.ImportFilesInternalAsync(files, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ImportFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        return this.ImportFileAsync(new Document(fileName), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> ImportFileAsync(string fileName, DocumentDetails details, CancellationToken cancellationToken = default)
    {
        var ids = await this.ImportFilesAsync(new[] { new Document(fileName) { Details = details } }, cancellationToken).ConfigureAwait(false);
        return ids.First();
    }

    /// <inheritdoc />
    public Task<MemoryAnswer> AskAsync(string query, MemoryFilter? filter = null, CancellationToken cancellationToken = default)
    {
        return this.AskAsync(new DocumentDetails().UserId, query, filter, cancellationToken);
    }

    /// <inheritdoc />
    public Task<MemoryAnswer> AskAsync(string userId, string query, MemoryFilter? filter = null, CancellationToken cancellationToken = default)
    {
        return this._searchClient.AskAsync(userId: userId, query: query, filter: filter, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsReadyAsync(string userId, string documentId, CancellationToken cancellationToken = default)
    {
        var orchestrator = await this.GetOrchestratorAsync(cancellationToken).ConfigureAwait(false);
        return await orchestrator.IsReadyAsync(userId, documentId, cancellationToken).ConfigureAwait(false);
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

    private async Task<IList<string>> ImportFilesInternalAsync(Document[] files, CancellationToken cancellationToken)
    {
        List<string> ids = new();
        InProcessPipelineOrchestrator orchestrator = await this.GetOrchestratorAsync(cancellationToken).ConfigureAwait(false);

        foreach (Document file in files)
        {
            var pipeline = orchestrator
                .PrepareNewFileUploadPipeline(
                    userId: file.Details.UserId,
                    documentId: file.Details.DocumentId,
                    file.Details.Tags);

            pipeline.AddUploadFile(
                name: "file1",
                filename: file.FileName,
                sourceFile: file.FileName);

            pipeline
                .Then("extract")
                .Then("partition")
                .Then("gen_embeddings")
                .Then("save_embeddings")
                .Build();

            await orchestrator.RunPipelineAsync(pipeline, cancellationToken).ConfigureAwait(false);
            ids.Add(file.Details.DocumentId);
        }

        return ids;
    }

    #endregion
}
