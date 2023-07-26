// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticKernel.SemanticMemory.Core.Configuration;
using Microsoft.SemanticKernel.SemanticMemory.Core.MemoryStorage;
using Microsoft.SemanticKernel.SemanticMemory.Core.Pipeline;

namespace Microsoft.SemanticKernel.SemanticMemory.Core.Handlers;

public class SaveEmbeddingsHandler : IPipelineStepHandler
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly List<object> _storages;
    private readonly ILogger<SaveEmbeddingsHandler> _log;

    /// <summary>
    /// Note: stepName and other params are injected with DI, <see cref="DependencyInjection.UseHandler{THandler}"/>
    /// </summary>
    /// <param name="stepName">Pipeline step for which the handler will be invoked</param>
    /// <param name="orchestrator">Current orchestrator used by the pipeline, giving access to content and other helps.</param>
    /// <param name="configuration">Application settings</param>
    /// <param name="log">Application logger</param>
    public SaveEmbeddingsHandler(
        string stepName,
        IPipelineOrchestrator orchestrator,
        SKMemoryConfig configuration,
        ILogger<SaveEmbeddingsHandler>? log = null)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;
        this._storages = new List<object>();
        this._log = log ?? NullLogger<SaveEmbeddingsHandler>.Instance;

        VectorStorageConfig storageConfig = configuration.GetHandlerConfig<VectorStorageConfig>(stepName, "VectorStorage");
        for (int index = 0; index < storageConfig.List.Count; index++)
        {
            this._storages.Add(storageConfig.GetListItem(index));
        }

        this._log.LogInformation("Handler ready: {0}. {1} vector storages.", stepName, this._storages.Count);
        if (this._storages.Count < 1)
        {
            this._log.LogWarning("No vector storage configured");
        }
    }

    /// <inheritdoc />
    public string StepName { get; }

    /// <inheritdoc />
    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(DataPipeline pipeline, CancellationToken cancellationToken)
    {
        // Loop through all the vector storages
        foreach (object storageConfig in this._storages)
        {
            switch (storageConfig)
            {
                case AzureCognitiveSearchConfig cfg:
                {
                    var result = await this.StoreInAzureCognitiveSearchAsync(cfg, pipeline, cancellationToken).ConfigureAwait(false);
                    if (!result.success)
                    {
                        return result;
                    }

                    break;
                }

                case QdrantConfig cfg:
                {
                    var result = await this.StoreInQdrantAsync(cfg, pipeline, cancellationToken).ConfigureAwait(false);
                    if (!result.success)
                    {
                        return result;
                    }

                    break;
                }
            }
        }

        return (true, pipeline);
    }

    public async Task<(bool success, DataPipeline updatedPipeline)> StoreInAzureCognitiveSearchAsync(
        AzureCognitiveSearchConfig config, DataPipeline pipeline, CancellationToken cancellationToken)
    {
        await Task.Delay(0, cancellationToken).ConfigureAwait(false);
        // TODO
        // * loop vaults
        // * loop embedding files
        // * link to blob
        // * multiple embedding types

        var client = new AzureCognitiveSearchMemory(config.Endpoint, config.APIKey);
        string indexNamePrefix = $"skmemory-{pipeline.UserId}";
        foreach (string vaultId in pipeline.VaultIds)
        {
        }

        return (true, pipeline);
    }

    public async Task<(bool success, DataPipeline updatedPipeline)> StoreInQdrantAsync(
        QdrantConfig config, DataPipeline pipeline, CancellationToken cancellationToken)
    {
        await Task.Delay(0, cancellationToken).ConfigureAwait(false);

        return (true, pipeline);
    }
}
