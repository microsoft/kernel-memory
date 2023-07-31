// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticMemory.Client;
using Microsoft.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.ContentStorage;
using Microsoft.SemanticMemory.Core.Diagnostics;
using Microsoft.SemanticMemory.Core.MemoryStorage;
using Microsoft.SemanticMemory.Core.Pipeline;

namespace Microsoft.SemanticMemory.Core.Handlers;

public class SaveEmbeddingsHandler : IPipelineStepHandler
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly List<object> _vectorDbs;
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
        SemanticMemoryConfig configuration,
        ILogger<SaveEmbeddingsHandler>? log = null)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;
        this._log = log ?? NullLogger<SaveEmbeddingsHandler>.Instance;
        this._vectorDbs = new List<object>();

        var handlerConfig = configuration.GetHandlerConfig<VectorStorageConfig>(stepName);
        for (int index = 0; index < handlerConfig.VectorDbs.Count; index++)
        {
            this._vectorDbs.Add(handlerConfig.GetVectorDbConfig(index));
        }

        this._log.LogInformation("Handler {0} ready, {1} vector storages", stepName, this._vectorDbs.Count);
        if (this._vectorDbs.Count < 1)
        {
            this._log.LogWarning("No vector storage configured");
        }
    }

    /// <inheritdoc />
    public string StepName { get; }

    /// <inheritdoc />
    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(DataPipeline pipeline, CancellationToken cancellationToken)
    {
        // For each embedding file => For each Vector DB => Store vector (collections ==> tags)
        foreach (KeyValuePair<string, DataPipeline.GeneratedFileDetails> embeddingFile in pipeline.Files.SelectMany(x => x.GeneratedFiles.Where(f => f.Value.IsEmbeddingFile())))
        {
            foreach (object storageConfig in this._vectorDbs)
            {
                string vectorJson = await this._orchestrator.ReadTextFileAsync(pipeline, embeddingFile.Value.Name, cancellationToken).ConfigureAwait(false);
                EmbeddingFileContent? embeddingData = JsonSerializer.Deserialize<EmbeddingFileContent>(vectorJson);
                if (embeddingData == null)
                {
                    throw new OrchestrationException($"Unable to deserialize embedding file {embeddingFile.Value.Name}");
                }

                var record = new MemoryRecord
                {
                    Id = $"u={pipeline.UserId}//p={pipeline.Id}//v={embeddingFile.Value.Id}",
                    SourceId = $"u={pipeline.UserId}//p={pipeline.Id}//v={embeddingFile.Value.ParentId}",
                    Vector = embeddingData.Vector,
                    Owner = pipeline.UserId,
                };

                record.Tags.Add(Constants.ReservedUserIdTag, pipeline.UserId);
                record.Tags.Add(Constants.ReservedDocIdTag, pipeline.Id);
                record.Tags.Add(Constants.ReservedFileIdTag, embeddingFile.Value.ParentId);
                record.Tags.Add(Constants.ReservedFilePartitionTag, embeddingFile.Value.Id);
                record.Tags.Add(Constants.ReservedFileTypeTag, pipeline.GetFile(embeddingFile.Value.ParentId).Type);

                pipeline.Tags.CopyTo(record.Tags);

                record.Metadata.Add("file_name", pipeline.GetFile(embeddingFile.Value.ParentId).Name);
                record.Metadata.Add("vector_provider", embeddingData.GeneratorProvider);
                record.Metadata.Add("vector_generator", embeddingData.GeneratorName);
                record.Metadata.Add("last_update", DateTimeOffset.UtcNow.ToString("s"));

                // Store text partition for RAG
                // TODO: make this optional to reduce space usage, using blob files instead
                string partitionContent = await this._orchestrator.ReadTextFileAsync(pipeline, embeddingData.SourceFileName, cancellationToken).ConfigureAwait(false);
                record.Metadata.Add("text", partitionContent);

                switch (storageConfig)
                {
                    case AzureCognitiveSearchConfig cfg:
                        await this.StoreInAzureCognitiveSearchAsync(cfg, record, embeddingData.GeneratorProvider, embeddingData.GeneratorName, cancellationToken).ConfigureAwait(false);
                        break;

                    case QdrantConfig cfg:
                        await this.StoreInQdrantAsync(cfg, record, embeddingData.GeneratorProvider, embeddingData.GeneratorName, cancellationToken).ConfigureAwait(false);
                        break;
                }
            }
        }

        return (true, pipeline);
    }

    public async Task StoreInAzureCognitiveSearchAsync(
        AzureCognitiveSearchConfig config,
        MemoryRecord record,
        string vectorProvider,
        string vectorGenerator,
        CancellationToken cancellationToken)
    {
        var client = new AzureCognitiveSearchMemory(config.Endpoint, config.APIKey);
        string indexName = $"smemory-{record.Owner}-{vectorProvider}-{vectorGenerator}";

        await client.CreateCollectionAsync(indexName, AzureCognitiveSearchMemoryRecord.GetSchema(record.Vector.Count), cancellationToken).ConfigureAwait(false);
        await client.UpsertAsync(indexName, record, cancellationToken).ConfigureAwait(false);
    }

    public async Task StoreInQdrantAsync(
        QdrantConfig config,
        MemoryRecord record,
        string vectorProvider,
        string vectorGenerator,
        CancellationToken cancellationToken)
    {
        await Task.Delay(0, cancellationToken).ConfigureAwait(false);
        throw new OrchestrationException("Qdrant not supported yet");
    }
}
