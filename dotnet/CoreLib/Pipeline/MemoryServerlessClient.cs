// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.Client;
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
        this._serviceProvider = AppBuilder.Build((serv, cfg) => { serv.UseSearchClient(cfg); }).Services;
        this._searchClient = this._serviceProvider.GetService<SearchClient>()
                             ?? throw new ConfigurationException(
                                 "Unable to load search client, the object is null. Are all the dependencies configured?");
    }

    /// <inheritdoc />
    public async Task<string> ImportFileAsync(Document file)
    {
        var ids = await this.ImportFilesAsync(new[] { file }).ConfigureAwait(false);
        return ids.First();
    }

    /// <inheritdoc />
    public Task<IList<string>> ImportFilesAsync(Document[] files)
    {
        return this.ImportFilesInternalAsync(files);
    }

    /// <inheritdoc />
    public Task<string> ImportFileAsync(string fileName)
    {
        return this.ImportFileAsync(new Document(fileName));
    }

    /// <inheritdoc />
    public async Task<string> ImportFileAsync(string fileName, DocumentDetails details)
    {
        var ids = await this.ImportFilesAsync(new[] { new Document(fileName) { Details = details } }).ConfigureAwait(false);
        return ids.First();
    }

    /// <inheritdoc />
    public Task<MemoryAnswer> AskAsync(string userId, string query)
    {
        return this._searchClient.SearchAsync(userId: userId, query: query);
    }

    public async Task<bool> ExistsAsync(string userId, string documentId)
    {
        // WORK IN PROGRESS
        await Task.Delay(0).ConfigureAwait(false);

        return false;
    }

    #region private

    private readonly SearchClient _searchClient;
    private InProcessPipelineOrchestrator? _inProcessOrchestrator;
    private IServiceProvider? _serviceProvider;

    private IServiceProvider GetServiceProvider()
    {
        if (this._serviceProvider == null)
        {
            this._serviceProvider = AppBuilder.Build((services, config) =>
            {
                services.UseSearchClient(config);
            }).Services;
        }

        return this._serviceProvider;
    }

#pragma warning disable CA2208
    private async Task<InProcessPipelineOrchestrator> GetOrchestratorAsync()
    {
        if (this._inProcessOrchestrator == null)
        {
            var orchestrator = this.GetServiceProvider().GetService<InProcessPipelineOrchestrator>();
            if (orchestrator == null)
            {
                throw new ArgumentNullException(nameof(orchestrator),
                    $"Unable to instantiate {typeof(InProcessPipelineOrchestrator)} with AppBuilder");
            }

            // Text extraction handler
            TextExtractionHandler textExtraction = new("extract", orchestrator);
            await orchestrator.AddHandlerAsync(textExtraction).ConfigureAwait(false);

            // Text partitioning handler
            TextPartitioningHandler textPartitioning = new("partition", orchestrator);
            await orchestrator.AddHandlerAsync(textPartitioning).ConfigureAwait(false);

            // Embedding generation handler
            GenerateEmbeddingsHandler textEmbedding = new("gen_embeddings", orchestrator, SemanticMemoryConfig.LoadFromAppSettings());
            await orchestrator.AddHandlerAsync(textEmbedding).ConfigureAwait(false);

            // Embedding storage handler
            SaveEmbeddingsHandler saveEmbedding = new("save_embeddings", orchestrator, SemanticMemoryConfig.LoadFromAppSettings());
            await orchestrator.AddHandlerAsync(saveEmbedding).ConfigureAwait(false);

            this._inProcessOrchestrator = orchestrator;
        }

        return this._inProcessOrchestrator;
    }
#pragma warning restore CA2208

    private async Task<IList<string>> ImportFilesInternalAsync(Document[] files)
    {
        List<string> ids = new();
        InProcessPipelineOrchestrator orchestrator = await this.GetOrchestratorAsync().ConfigureAwait(false);

        foreach (Document file in files)
        {
            var pipeline = orchestrator
                .PrepareNewFileUploadPipeline(
                    documentId: file.Details.DocumentId,
                    userId: file.Details.UserId, file.Details.Tags);

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

            await orchestrator.RunPipelineAsync(pipeline).ConfigureAwait(false);
            ids.Add(file.Details.DocumentId);
        }

        return ids;
    }

    // private static IServiceProvider LoadServiceProviderWithSearchClient()
    // {
    //     return AppBuilder.Build((services, config) =>
    //     {
    //         services.UseSearchClient(config);
    //     }).Services;
    // }

    // private static SearchClient BuildSearchClient()
    // {
    //     var client = _serviceProvider.Value.GetService<SearchClient>();
    //     if (client == null)
    //     {
    //         throw new SemanticMemoryException("Unable to load search client, object is NULL");
    //     }
    //
    //     return client;
    // }

    #endregion
}
