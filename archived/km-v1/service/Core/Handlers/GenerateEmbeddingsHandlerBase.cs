// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.DocumentStorage;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.Handlers;

public abstract class GenerateEmbeddingsHandlerBase
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly ILogger _log;

    protected abstract IPipelineStepHandler ActualInstance { get; }

    protected GenerateEmbeddingsHandlerBase(IPipelineOrchestrator orchestrator, ILogger log)
    {
        this._orchestrator = orchestrator;
        this._log = log;
    }

    protected async Task<List<PartitionInfo>> GetListOfPartitionsToProcessAsync(
        DataPipeline pipeline,
        string subStepName,
        CancellationToken cancellationToken)
    {
        var partitionsToProcess = new List<PartitionInfo>();

        this._log.LogTrace("Generating list of files to process, pipeline '{0}/{1}', sub-step '{2}'",
            pipeline.Index, pipeline.DocumentId, subStepName);
        foreach (DataPipeline.FileDetails uploadedFile in pipeline.Files)
        {
            foreach (KeyValuePair<string, DataPipeline.GeneratedFileDetails> generatedFile in uploadedFile.GeneratedFiles)
            {
                DataPipeline.GeneratedFileDetails partitionFile = generatedFile.Value;

                // Calc embeddings only for partitions (text chunks) and synthetic data
                if (partitionFile.ArtifactType is not DataPipeline.ArtifactTypes.TextPartition
                    and not DataPipeline.ArtifactTypes.SyntheticData)
                {
                    this._log.LogTrace("Skipping file {0} (not a partition, not synthetic data)", partitionFile.Name);
                    continue;
                }

                // Skip text partitions already processed by this handler+generator
                if (partitionFile.AlreadyProcessedBy(this.ActualInstance, subStepName))
                {
                    this._log.LogTrace("File {0} already processed by this handler (sub-step {1})", partitionFile.Name, subStepName);
                    continue;
                }

                // TODO: cost/perf: if the partition SHA256 is the same and the embedding exists, avoid generating it again
                switch (partitionFile.MimeType)
                {
                    case MimeTypes.PlainText:
                    case MimeTypes.MarkDown:
                        // TODO: handle Azure.RequestFailedException - BlobNotFound
                        var partitionContent = await this._orchestrator.ReadTextFileAsync(pipeline, partitionFile.Name, cancellationToken).ConfigureAwait(false);
                        partitionsToProcess.Add(new PartitionInfo(generatedFile, uploadedFile, partitionContent));
                        break;

                    default:
                        this._log.LogWarning("File {0} cannot be used to generate embeddings, type not supported", partitionFile.Name);
                        continue;
                }
            }
        }

        return partitionsToProcess;
    }

    // Store embeddings in Azure Blobs/Disk/S3
    protected async Task SaveEmbeddingsToDocumentStorageAsync(
        DataPipeline pipeline,
        PartitionInfo[] partitions,
        Embedding[] embeddings,
        string generatorProvider,
        string generatorName,
        CancellationToken cancellationToken)
    {
        if (partitions.Length != embeddings.Length)
        {
            throw new ArgumentException("The list of embeddings doesn't match the list of text partitions. The two lists have different size: " +
                                        $"{embeddings.Length} embeddings != {partitions.Length} text partitions.");
        }

        for (int i = 0; i < partitions.Length; i++)
        {
            await this.SaveEmbeddingToDocumentStorageAsync(
                    pipeline, partitions[i], embeddings[i], generatorProvider, generatorName, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    // Store embedding in Azure Blobs/Disk/S3
    protected async Task SaveEmbeddingToDocumentStorageAsync(
        DataPipeline pipeline,
        PartitionInfo partition,
        Embedding embedding,
        string generatorProvider,
        string generatorName,
        CancellationToken cancellationToken)
    {
        // This is the file containing the text chunk. In future this will include also chunk metadata
        DataPipeline.GeneratedFileDetails partitionFile = partition.GeneratedFile.Value;

        // This is the data stored in document storage, for each embedding.
        EmbeddingFileContent embeddingData = new()
        {
            SourceFileName = partitionFile.Name,
            GeneratorProvider = generatorProvider,
            GeneratorName = generatorName,
            Vector = embedding,
            VectorSize = embedding.Length,
            TimeStamp = DateTimeOffset.UtcNow
        };

        string embeddingDataAsJson = JsonSerializer.Serialize(embeddingData);
        string embeddingDataFileName = GetEmbeddingFileName(partitionFile.Name, generatorProvider, generatorName);
        await this._orchestrator.WriteTextFileAsync(pipeline, embeddingDataFileName, embeddingDataAsJson, cancellationToken).ConfigureAwait(false);

        this.TrackNewFileInPipelineStatus(
            newFileName: embeddingDataFileName,
            newFileSize: embeddingDataAsJson.Length,
            sourcePartitionFile: partitionFile,
            sourceUserFile: partition.UploadedFile);

        partition.GeneratedFile.Value.MarkProcessedBy(this.ActualInstance, GetSubStepName(generatorProvider, generatorName));
    }

    protected static string GetSubStepName(object generator)
    {
        return GetSubStepName(GetEmbeddingProviderName(generator), GetEmbeddingGeneratorName(generator));
    }

    protected static string GetSubStepName(string providerName, string generatorName)
    {
        return $"{providerName}/{generatorName}";
    }

    protected static string GetEmbeddingProviderName(object generator)
    {
        var generatorProviderClassName = generator.GetType().FullName ?? generator.GetType().Name;
        return string.Join('.', generatorProviderClassName.Split('.').TakeLast(3));
    }

    protected static string GetEmbeddingGeneratorName(object generator)
    {
        /* @todo Embedding cache
         *
         * The orchestrator is caching embeddings, and the cache key would be composed by:
         *
         * 1. the generator class name (see GetEmbeddingProviderName)
         * 2. the model used
         *
         * Embedding generators do not expose the model in use though, so we're using a
         * temporary placeholder.
         *
         * Work to do: remove embedding cache from the pipeline and leave it to embedding
         * generators to cache (dev branch: embeddingcache), so that all clients and handlers
         * will benefit. This approach removes also the need for generators to expose
         * internal details.
         */

        return "__";
    }

    protected class PartitionInfo(
        KeyValuePair<string, DataPipeline.GeneratedFileDetails> generatedFile,
        DataPipeline.FileDetails uploadedFile,
        string partitionContent)
    {
        public KeyValuePair<string, DataPipeline.GeneratedFileDetails> GeneratedFile { get; set; } = generatedFile;
        public DataPipeline.FileDetails UploadedFile { get; set; } = uploadedFile;
        public string PartitionContent { get; set; } = partitionContent;
    }

    #region private =========================================================================================

    // Add new files to pipeline status, under the current partition file being processed
    private void TrackNewFileInPipelineStatus(
        string newFileName,
        int newFileSize,
        DataPipeline.GeneratedFileDetails sourcePartitionFile,
        DataPipeline.FileDetails sourceUserFile)
    {
        var newFileDetails = new DataPipeline.GeneratedFileDetails
        {
            Id = Guid.NewGuid().ToString("N"),
            ParentId = sourceUserFile.Id,
            SourcePartitionId = sourcePartitionFile.Id,
            Name = newFileName,
            Size = newFileSize,
            MimeType = MimeTypes.TextEmbeddingVector,
            ArtifactType = DataPipeline.ArtifactTypes.TextEmbeddingVector,
            PartitionNumber = sourcePartitionFile.PartitionNumber,
            SectionNumber = sourcePartitionFile.SectionNumber,
            Tags = sourcePartitionFile.Tags,
        };

        newFileDetails.MarkProcessedBy(this.ActualInstance);

        // Add new files to pipeline status, under the file uploaded by the user
        lock (sourceUserFile.GeneratedFiles)
        {
            sourceUserFile.GeneratedFiles.Add(newFileName, newFileDetails);
        }
    }

    private static string GetEmbeddingFileName(string srcFilename, string type, string embeddingName)
    {
        return $"{srcFilename}.{type}.{embeddingName}{FileExtensions.TextEmbeddingVector}";
    }

    #endregion
}
