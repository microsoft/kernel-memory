// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.ContentStorage;
using Microsoft.SemanticMemory.Diagnostics;
using Microsoft.SemanticMemory.MemoryStorage;
using Microsoft.SemanticMemory.Pipeline;

namespace Microsoft.SemanticMemory.Handlers;

public class SaveEmbeddingsHandler : IPipelineStepHandler
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly List<ISemanticMemoryVectorDb> _vectorDbs;
    private readonly ILogger<SaveEmbeddingsHandler> _log;

    /// <inheritdoc />
    public string StepName { get; }

    /// <summary>
    /// Handler responsible for copying embeddings from storage to list of vector DBs
    /// Note: stepName and other params are injected with DI
    /// </summary>
    /// <param name="stepName">Pipeline step for which the handler will be invoked</param>
    /// <param name="orchestrator">Current orchestrator used by the pipeline, giving access to content and other helps.</param>
    /// <param name="log">Application logger</param>
    public SaveEmbeddingsHandler(
        string stepName,
        IPipelineOrchestrator orchestrator,
        ILogger<SaveEmbeddingsHandler>? log = null)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;
        this._log = log ?? DefaultLogger<SaveEmbeddingsHandler>.Instance;
        this._vectorDbs = orchestrator.GetVectorDbs();

        this._log.LogInformation("Handler {0} ready, {1} vector storages", stepName, this._vectorDbs.Count);
        if (this._vectorDbs.Count < 1)
        {
            this._log.LogError("No vector storage configured");
        }
    }

    /// <inheritdoc />
    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        await this.DeletePreviousEmbeddingsAsync(pipeline, cancellationToken).ConfigureAwait(false);
        pipeline.PreviousExecutionsToPurge = new List<DataPipeline>();

        // For each embedding file => For each Vector DB => Store vector (collections ==> tags)
        foreach (KeyValuePair<string, DataPipeline.GeneratedFileDetails> embeddingFile in
                 pipeline.Files.SelectMany(x => x.GeneratedFiles.Where(f => f.Value.ArtifactType == DataPipeline.ArtifactTypes.TextEmbeddingVector)))
        {
            if (embeddingFile.Value.AlreadyProcessedBy(this))
            {
                this._log.LogTrace("File {0} already processed by this handler", embeddingFile.Value.Name);
                continue;
            }

            string vectorJson = await this._orchestrator.ReadTextFileAsync(pipeline, embeddingFile.Value.Name, cancellationToken).ConfigureAwait(false);
            EmbeddingFileContent? embeddingData = JsonSerializer.Deserialize<EmbeddingFileContent>(vectorJson);
            if (embeddingData == null)
            {
                throw new OrchestrationException($"Unable to deserialize embedding file {embeddingFile.Value.Name}");
            }

            var record = new MemoryRecord
            {
                Id = GetEmbeddingRecordId(pipeline.DocumentId, embeddingFile.Value.Id),
                Vector = embeddingData.Vector,
            };

            // Note that the User Id is not set here, but when mapping MemoryRecord to the specific VectorDB schema 
            record.Tags.Add(Constants.ReservedDocumentIdTag, pipeline.DocumentId);
            record.Tags.Add(Constants.ReservedFileIdTag, embeddingFile.Value.ParentId);
            record.Tags.Add(Constants.ReservedFilePartitionTag, embeddingFile.Value.Id);
            record.Tags.Add(Constants.ReservedFileTypeTag, pipeline.GetFile(embeddingFile.Value.ParentId).MimeType);

            pipeline.Tags.CopyTo(record.Tags);

            record.Payload.Add(Constants.ReservedPayloadFileNameField, pipeline.GetFile(embeddingFile.Value.ParentId).Name);
            record.Payload.Add(Constants.ReservedPayloadEmbeddingSrcFileNameField, embeddingData.SourceFileName);
            record.Payload.Add(Constants.ReservedPayloadLastUpdateField, DateTimeOffset.UtcNow.ToString("s"));
            record.Payload.Add(Constants.ReservedPayloadVectorProviderField, embeddingData.GeneratorProvider);
            record.Payload.Add(Constants.ReservedPayloadVectorGeneratorField, embeddingData.GeneratorName);

            // Store text partition for RAG
            // TODO: make this optional to reduce space usage, using blob files instead
            string partitionContent = await this._orchestrator.ReadTextFileAsync(pipeline, embeddingData.SourceFileName, cancellationToken).ConfigureAwait(false);
            record.Payload.Add(Constants.ReservedPayloadTextField, partitionContent);

            foreach (ISemanticMemoryVectorDb client in this._vectorDbs)
            {
                this._log.LogTrace("Creating index '{0}'", pipeline.Index);
                await client.CreateIndexAsync(pipeline.Index, record.Vector.Count, cancellationToken).ConfigureAwait(false);

                this._log.LogTrace("Saving record {0} in index '{1}'", record.Id, pipeline.Index);
                await client.UpsertAsync(pipeline.Index, record, cancellationToken).ConfigureAwait(false);
            }

            embeddingFile.Value.MarkProcessedBy(this);
        }

        return (true, pipeline);
    }

    private async Task DeletePreviousEmbeddingsAsync(DataPipeline pipeline, CancellationToken cancellationToken)
    {
        if (pipeline.PreviousExecutionsToPurge.Count == 0) { return; }

        var embeddingsToKeep = new HashSet<string>();

        // Decide which embeddings not to delete, looking at the current pipeline
        foreach (DataPipeline.GeneratedFileDetails embeddingFile
                 in pipeline.Files.SelectMany(f1 => f1.GeneratedFiles.Where(f2 => f2.Value.ArtifactType == DataPipeline.ArtifactTypes.TextEmbeddingVector).Select(x => x.Value)))
        {
            string recordId = GetEmbeddingRecordId(pipeline.DocumentId, embeddingFile.Id);
            embeddingsToKeep.Add(recordId);
        }

        // Purge old pipelines data, unless it's still relevant in the current pipeline
        foreach (DataPipeline oldPipeline in pipeline.PreviousExecutionsToPurge)
        {
            foreach (DataPipeline.GeneratedFileDetails embeddingFile
                     in oldPipeline.Files.SelectMany(f1 => f1.GeneratedFiles.Where(f2 => f2.Value.ArtifactType == DataPipeline.ArtifactTypes.TextEmbeddingVector).Select(x => x.Value)))
            {
                string recordId = GetEmbeddingRecordId(oldPipeline.DocumentId, embeddingFile.Id);
                if (embeddingsToKeep.Contains(recordId)) { continue; }

                foreach (ISemanticMemoryVectorDb client in this._vectorDbs)
                {
                    this._log.LogTrace("Deleting old embedding {0}", recordId);
                    await client.DeleteAsync(pipeline.Index, new MemoryRecord { Id = recordId }, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    private static string GetEmbeddingRecordId(string pipelineId, string filePartitionId)
    {
        // Note: this value is serialized in different ways depending on the vector DB, so you
        // can search for: tags[] contains "__document_id:{pipelineId}" && tags[] contains "__file_part={filePartitionId}"
        // e.g. $filter=tags/any(s: s eq '__document_id:doc001')&$select=tags,payload
        return $"pi={pipelineId}//fpi={filePartitionId}";
    }
}
