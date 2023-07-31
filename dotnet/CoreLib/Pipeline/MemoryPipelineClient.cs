// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Client;
using Microsoft.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.Diagnostics;
using Microsoft.SemanticMemory.Core.Handlers;

namespace Microsoft.SemanticMemory.Core.Pipeline;

public class MemoryPipelineClient : ISemanticMemoryClient
{
    public MemoryPipelineClient() : this(SemanticMemoryConfig.LoadFromAppSettings())
    {
    }

    public MemoryPipelineClient(SemanticMemoryConfig config)
    {
        this._config = config;
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
    public async Task<string> AskAsync(string question, string userId)
    {
        // Work in progress

        await Task.Delay(0).ConfigureAwait(false);

        return "...work in progress...";
    }

    #region private

    private readonly SemanticMemoryConfig _config;
    private readonly Lazy<Task<InProcessPipelineOrchestrator>> _inProcessOrchestrator = new(BuildInProcessOrchestratorAsync);

    private Task<InProcessPipelineOrchestrator> Orchestrator
    {
        get { return this._inProcessOrchestrator.Value; }
    }

    private async Task<IList<string>> ImportFilesInternalAsync(Document[] files)
    {
        List<string> ids = new();
        InProcessPipelineOrchestrator orchestrator = await this.Orchestrator.ConfigureAwait(false);

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

    private static async Task<InProcessPipelineOrchestrator> BuildInProcessOrchestratorAsync()
    {
        IServiceProvider services = AppBuilder.Build().Services;

        var orchestrator = GetOrchestrator(services);

        // Text extraction handler
        TextExtractionHandler textExtraction = new("extract",
            orchestrator, GetLogger<TextExtractionHandler>(services));
        await orchestrator.AddHandlerAsync(textExtraction).ConfigureAwait(false);

        // Text partitioning handler
        TextPartitioningHandler textPartitioning = new("partition",
            orchestrator, GetLogger<TextPartitioningHandler>(services));
        await orchestrator.AddHandlerAsync(textPartitioning).ConfigureAwait(false);

        // Embedding generation handler
        GenerateEmbeddingsHandler textEmbedding = new("gen_embeddings",
            orchestrator, GetConfig(services), GetLogger<GenerateEmbeddingsHandler>(services));
        await orchestrator.AddHandlerAsync(textEmbedding).ConfigureAwait(false);

        // Embedding storage handler
        SaveEmbeddingsHandler saveEmbedding = new("save_embeddings",
            orchestrator, GetConfig(services), GetLogger<SaveEmbeddingsHandler>(services));
        await orchestrator.AddHandlerAsync(saveEmbedding).ConfigureAwait(false);

        return orchestrator;
    }

    private static InProcessPipelineOrchestrator GetOrchestrator(IServiceProvider services)
    {
        var orchestrator = services.GetService<InProcessPipelineOrchestrator>();
        if (orchestrator == null)
        {
#pragma warning disable CA2208
            throw new ArgumentNullException(nameof(orchestrator),
                $"Unable to instantiate {typeof(InProcessPipelineOrchestrator)} with AppBuilder");
#pragma warning restore CA2208
        }

        return orchestrator;
    }

    private static SemanticMemoryConfig GetConfig(IServiceProvider services)
    {
        var config = services.GetService<SemanticMemoryConfig>();
        if (config == null)
        {
            throw new OrchestrationException("Unable to load configuration, object is NULL");
        }

        return config;
    }

    private static ILogger<T>? GetLogger<T>(IServiceProvider services)
    {
        return services.GetService<ILogger<T>>();
    }

    #endregion
}
