// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.Client;
using Microsoft.SemanticMemory.Client.Models;
using Microsoft.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.Handlers;
using Microsoft.SemanticMemory.Core.Search;

namespace Microsoft.SemanticMemory.Core.Pipeline;

/// <summary>
/// Memory client to upload files and search for answers, without depending
/// on a web service. By design this class is hardcoded to use
/// <see cref="InProcessPipelineOrchestrator"/>, hence the name "Serverless".
/// The class accesses directly storage, vectors and AI.
///
/// TODO: check if DI is needed
/// TODO: pipeline structure is hardcoded, should allow custom handlers/steps
/// </summary>
public class MemoryServerlessClient : ISemanticMemoryClient
{
    public MemoryServerlessClient(IServiceProvider serviceProvider)
    {
        this._serviceProvider = serviceProvider;
        this._searchClient = this._serviceProvider.GetService<SearchClient>()
                             ?? throw new ConfigurationException(
                                 "Unable to load search client, the object is null. Are all the dependencies configured?");
    }

    public MemoryServerlessClient()
    {
        this._serviceProvider = AppBuilder.Build((serv, cfg) => { serv.UseSearchClient(); }).Services;
        this._searchClient = this._serviceProvider.GetService<SearchClient>()
                             ?? throw new ConfigurationException(
                                 "Unable to load search client, the object is null. Are all the dependencies configured?");
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
    public Task<MemoryAnswer> AskAsync(string query, CancellationToken cancellationToken = default)
    {
        return this.AskAsync(new DocumentDetails().UserId, query, cancellationToken);
    }

    /// <inheritdoc />
    public Task<MemoryAnswer> AskAsync(string userId, string query, CancellationToken cancellationToken = default)
    {
        return this._searchClient.SearchAsync(userId: userId, query: query);
    }

    /// <inheritdoc />
    public async Task<bool> IsReadyAsync(string userId, string documentId, CancellationToken cancellationToken = default)
    {
        var orchestrator = await this.GetOrchestratorAsync(cancellationToken).ConfigureAwait(false);
        DataPipeline? pipeline = await orchestrator.ReadPipelineStatusAsync(userId, documentId, cancellationToken).ConfigureAwait(false);

        return pipeline != null && pipeline.Complete;
    }

    #region private

    private readonly SearchClient _searchClient;
    private InProcessPipelineOrchestrator? _inProcessOrchestrator;
    private IServiceProvider _serviceProvider;

    private IServiceProvider GetServiceProvider()
    {
        if (this._serviceProvider == null)
        {
            this._serviceProvider = AppBuilder.Build((services, config) =>
            {
                services.UseSearchClient();
            }).Services;
        }

        return this._serviceProvider;
    }

#pragma warning disable CA2208
    private async Task<InProcessPipelineOrchestrator> GetOrchestratorAsync(CancellationToken cancellationToken)
    {
        if (this._inProcessOrchestrator == null)
        {
            var orchestrator = this._serviceProvider.GetService<InProcessPipelineOrchestrator>();
            if (orchestrator == null)
            {
                throw new ArgumentNullException(nameof(orchestrator),
                    $"Unable to instantiate {typeof(InProcessPipelineOrchestrator)} with AppBuilder");
            }

            // Text extraction handler
            TextExtractionHandler textExtraction = new("extract", orchestrator);
            await orchestrator.AddHandlerAsync(textExtraction, cancellationToken).ConfigureAwait(false);

            // Text partitioning handler
            TextPartitioningHandler textPartitioning = new("partition", orchestrator);
            await orchestrator.AddHandlerAsync(textPartitioning, cancellationToken).ConfigureAwait(false);

            // Embedding generation handler
            GenerateEmbeddingsHandler textEmbedding = new("gen_embeddings", orchestrator, SemanticMemoryConfig.LoadFromAppSettings());
            await orchestrator.AddHandlerAsync(textEmbedding, cancellationToken).ConfigureAwait(false);

            // Embedding storage handler
            SaveEmbeddingsHandler saveEmbedding = new("save_embeddings", orchestrator, SemanticMemoryConfig.LoadFromAppSettings());
            await orchestrator.AddHandlerAsync(saveEmbedding, cancellationToken).ConfigureAwait(false);

            this._inProcessOrchestrator = orchestrator;
        }

        return this._inProcessOrchestrator;
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
