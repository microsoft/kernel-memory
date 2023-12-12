// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.ContentStorage;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.Handlers;

public class SaveRecordsHandler : IPipelineStepHandler
{
    private class FileDetailsWithRecordId
    {
        public string RecordId { get; set; }

        public DataPipeline.GeneratedFileDetails File { get; set; }

        public FileDetailsWithRecordId(DataPipeline pipeline, DataPipeline.GeneratedFileDetails file)
        {
            this.File = file;
            this.RecordId = GetRecordId(pipeline.DocumentId, file.Id);
        }

        private static string GetRecordId(string documentId, string partId)
        {
            // Note: this value is serialized in different ways depending on the memory DB,
            // don't use it for search, and use tags instead e.g.
            //  Filtering by document ID:
            //  - $filter=tags/any(s: s eq '__document_id:doc001')&$select=tags,payload
            //  Filtering by file ID (a document can contain multiple files):
            //  - $filter=tags/any(s: s eq '__file_id:bfd793c29f1642c2b085a11ccc38f27d')&$select=tags,payload
            //  Filtering by chunk/partition ID:
            //  - $filter=tags/any(s: s eq '__file_part:6479a127dbcc38f3c085b1c2c29f1fd2')&$select=tags,payload
            return $"d={documentId}//p={partId}";
        }
    }

    private readonly IPipelineOrchestrator _orchestrator;
    private readonly List<IMemoryDb> _memoryDbs;
    private readonly ILogger<SaveRecordsHandler> _log;
    private readonly bool _embeddingGenerationEnabled;

    /// <inheritdoc />
    public string StepName { get; }

    /// <summary>
    /// Handler responsible for copying embeddings from storage to list of memory DBs
    /// Note: stepName and other params are injected with DI
    /// </summary>
    /// <param name="stepName">Pipeline step for which the handler will be invoked</param>
    /// <param name="orchestrator">Current orchestrator used by the pipeline, giving access to content and other helps.</param>
    /// <param name="log">Application logger</param>
    public SaveRecordsHandler(
        string stepName,
        IPipelineOrchestrator orchestrator,
        ILogger<SaveRecordsHandler>? log = null)
    {
        this.StepName = stepName;
        this._log = log ?? DefaultLogger<SaveRecordsHandler>.Instance;
        this._embeddingGenerationEnabled = orchestrator.EmbeddingGenerationEnabled;

        this._orchestrator = orchestrator;
        this._memoryDbs = orchestrator.GetMemoryDbs();

        if (this._memoryDbs.Count < 1)
        {
            this._log.LogError("Handler {0} NOT ready, no memory DB configured", stepName);
        }
        else
        {
            this._log.LogInformation("Handler {0} ready, {1} vector storages", stepName, this._memoryDbs.Count);
        }
    }

    /// <inheritdoc />
    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Saving memory records, pipeline '{0}/{1}'", pipeline.Index, pipeline.DocumentId);

        await this.DeletePreviousRecordsAsync(pipeline, cancellationToken).ConfigureAwait(false);
        pipeline.PreviousExecutionsToPurge = new List<DataPipeline>();

        return this._embeddingGenerationEnabled
            ? await this.SaveEmbeddingsAsync(pipeline, cancellationToken).ConfigureAwait(false)
            : await this.SavePartitionsAsync(pipeline, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Loop through all the EMBEDDINGS generated, creating a memory record for each one
    /// </summary>
    public async Task<(bool success, DataPipeline updatedPipeline)> SaveEmbeddingsAsync(
        DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        // For each embedding file => For each Memory DB => Upsert record
        foreach (FileDetailsWithRecordId file in GetListOfEmbeddingFiles(pipeline))
        {
            if (file.File.AlreadyProcessedBy(this))
            {
                this._log.LogTrace("File {0} already processed by this handler", file.File.Name);
                continue;
            }

            string vectorJson = await this._orchestrator.ReadTextFileAsync(pipeline, file.File.Name, cancellationToken).ConfigureAwait(false);
            EmbeddingFileContent? embeddingData = JsonSerializer.Deserialize<EmbeddingFileContent>(vectorJson.RemoveBOM().Trim());
            if (embeddingData == null)
            {
                throw new OrchestrationException($"Unable to deserialize embedding file {file.File.Name}");
            }

            string partitionContent = await this._orchestrator.ReadTextFileAsync(pipeline, embeddingData.SourceFileName, cancellationToken).ConfigureAwait(false);

            var record = PrepareRecord(
                pipeline: pipeline,
                recordId: file.RecordId,
                fileName: pipeline.GetFile(file.File.ParentId).Name,
                fileId: file.File.ParentId,
                partitionFileId: file.File.SourcePartitionId,
                partitionContent: partitionContent,
                partitionEmbedding: embeddingData.Vector,
                embeddingGeneratorProvider: embeddingData.GeneratorProvider,
                embeddingGeneratorName: embeddingData.GeneratorName,
                file.File.Tags);

            foreach (IMemoryDb client in this._memoryDbs)
            {
                this._log.LogTrace("Creating index '{0}'", pipeline.Index);
                await client.CreateIndexAsync(pipeline.Index, record.Vector.Length, cancellationToken).ConfigureAwait(false);

                this._log.LogTrace("Saving record {0} in index '{1}'", record.Id, pipeline.Index);
                await client.UpsertAsync(pipeline.Index, record, cancellationToken).ConfigureAwait(false);
            }

            file.File.MarkProcessedBy(this);
        }

        return (true, pipeline);
    }

    /// <summary>
    /// Loop through all the PARTITIONS and SYNTHETIC chunks, creating a memory record for each one
    /// </summary>
    public async Task<(bool success, DataPipeline updatedPipeline)> SavePartitionsAsync(
        DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        // Create records only for partitions (text chunks) and synthetic data
        foreach (FileDetailsWithRecordId file in GetListOfPartitionAndSyntheticFiles(pipeline))
        {
            if (file.File.AlreadyProcessedBy(this))
            {
                this._log.LogTrace("File {0} already processed by this handler", file.File.Name);
                continue;
            }

            switch (file.File.MimeType)
            {
                case MimeTypes.PlainText:
                case MimeTypes.MarkDown:

                    string partitionContent = await this._orchestrator.ReadTextFileAsync(pipeline, file.File.Name, cancellationToken).ConfigureAwait(false);

                    var record = PrepareRecord(
                        pipeline: pipeline,
                        recordId: file.RecordId,
                        fileName: pipeline.GetFile(file.File.ParentId).Name,
                        fileId: file.File.ParentId,
                        partitionFileId: file.File.Id,
                        partitionContent: partitionContent,
                        partitionEmbedding: new Embedding(),
                        embeddingGeneratorProvider: "",
                        embeddingGeneratorName: "",
                        file.File.Tags);

                    foreach (IMemoryDb client in this._memoryDbs)
                    {
                        this._log.LogTrace("Creating index '{0}'", pipeline.Index);
                        await client.CreateIndexAsync(pipeline.Index, record.Vector.Length, cancellationToken).ConfigureAwait(false);

                        this._log.LogTrace("Saving record {0} in index '{1}'", record.Id, pipeline.Index);
                        await client.UpsertAsync(pipeline.Index, record, cancellationToken).ConfigureAwait(false);
                    }

                    break;

                default:
                    this._log.LogWarning("File {0} cannot be used to generate embedding, type not supported", file.File.Name);
                    continue;
            }

            file.File.MarkProcessedBy(this);
        }

        return (true, pipeline);
    }

    private async Task DeletePreviousRecordsAsync(DataPipeline pipeline, CancellationToken cancellationToken)
    {
        if (pipeline.PreviousExecutionsToPurge.Count == 0) { return; }

        var recordsToKeep = new HashSet<string>();

        // Decide which records not to delete, looking at the current pipeline
        foreach (FileDetailsWithRecordId embeddingFile in GetListOfEmbeddingFiles(pipeline).Concat(GetListOfPartitionAndSyntheticFiles(pipeline)))
        {
            recordsToKeep.Add(embeddingFile.RecordId);
        }

        // Purge old pipelines data, unless it's still relevant in the current pipeline
        foreach (DataPipeline oldPipeline in pipeline.PreviousExecutionsToPurge)
        {
            foreach (FileDetailsWithRecordId file in GetListOfEmbeddingFiles(oldPipeline).Concat(GetListOfPartitionAndSyntheticFiles(oldPipeline)))
            {
                if (recordsToKeep.Contains(file.RecordId)) { continue; }

                foreach (IMemoryDb client in this._memoryDbs)
                {
                    this._log.LogTrace("Deleting old record {0}", file.RecordId);
                    await client.DeleteAsync(pipeline.Index, new MemoryRecord { Id = file.RecordId }, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    private static IEnumerable<FileDetailsWithRecordId> GetListOfEmbeddingFiles(DataPipeline pipeline)
    {
        return pipeline.Files.SelectMany(f1 => f1.GeneratedFiles.Where(
                f2 => f2.Value.ArtifactType == DataPipeline.ArtifactTypes.TextEmbeddingVector)
            .Select(x => new FileDetailsWithRecordId(pipeline, x.Value)));
    }

    private static IEnumerable<FileDetailsWithRecordId> GetListOfPartitionAndSyntheticFiles(DataPipeline pipeline)
    {
        return pipeline.Files.SelectMany(f1 => f1.GeneratedFiles.Where(
                f2 => f2.Value.ArtifactType == DataPipeline.ArtifactTypes.TextPartition || f2.Value.ArtifactType == DataPipeline.ArtifactTypes.SyntheticData)
            .Select(x => new FileDetailsWithRecordId(pipeline, x.Value)));
    }

    private static MemoryRecord PrepareRecord(
        DataPipeline pipeline,
        string recordId,
        string fileName,
        string fileId,
        string partitionFileId,
        string partitionContent,
        Embedding partitionEmbedding,
        string embeddingGeneratorProvider,
        string embeddingGeneratorName,
        TagCollection tags)
    {
        var record = new MemoryRecord { Id = recordId };

        /*
         * DOCUMENT DETAILS
         */

        // Document ID provided by the user, e.g. "my-doc-001"
        record.Tags.Add(Constants.ReservedDocumentIdTag, pipeline.DocumentId);

        /*
         * FILE DETAILS
         */

        // Original file name, useful for context, e.g. "NASA-August-2043.pdf"
        record.Payload.Add(Constants.ReservedPayloadFileNameField, fileName);

        // File type, e.g. "application/pdf" - Can be used for filtering by file type
        record.Tags.Add(Constants.ReservedFileTypeTag, pipeline.GetFile(fileId).MimeType);

        // File ID assigned by the system, e.g. "f00f0b3116ae423db22ebc80302b129c". New GUID generated by the orchestrator.
        // Can be used for filtering and deletions/purge
        record.Tags.Add(Constants.ReservedFileIdTag, fileId);

        /*
         * PARTITION DETAILS
         */

        record.Vector = partitionEmbedding;
        record.Payload.Add(Constants.ReservedPayloadTextField, partitionContent);
        record.Payload.Add(Constants.ReservedPayloadVectorProviderField, embeddingGeneratorProvider);
        record.Payload.Add(Constants.ReservedPayloadVectorGeneratorField, embeddingGeneratorName);

        // Partition ID. Filtering used for purge.
        record.Tags.Add(Constants.ReservedFilePartitionTag, partitionFileId);

        /*
         * TIMESTAMP and USER TAGS
         */

        record.Payload.Add(Constants.ReservedPayloadLastUpdateField, DateTimeOffset.UtcNow.ToString("s"));

        tags.CopyTo(record.Tags);

        return record;
    }
}
