// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.DocumentStorage;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.Handlers;

/// <summary>
/// Memory ingestion pipeline handler responsible for generating text embedding and saving them to the document storage.
/// </summary>
public sealed class GenerateEmbeddingsParallelHandler : IPipelineStepHandler
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly ILogger<GenerateEmbeddingsParallelHandler> _log;
    private readonly List<ITextEmbeddingGenerator> _embeddingGenerators;
    private readonly bool _embeddingGenerationEnabled;

    /// <inheritdoc />
    public string StepName { get; }

    /// <summary>
    /// Handler responsible for generating embeddings and saving them to document storage (not memory db).
    /// Note: stepName and other params are injected with DI
    /// </summary>
    /// <param name="stepName">Pipeline step for which the handler will be invoked</param>
    /// <param name="orchestrator">Current orchestrator used by the pipeline, giving access to content and other helps.</param>
    /// <param name="log">Application logger</param>
    public GenerateEmbeddingsParallelHandler(
        string stepName,
        IPipelineOrchestrator orchestrator,
        ILogger<GenerateEmbeddingsParallelHandler>? log = null)
    {
        this.StepName = stepName;
        this._log = log ?? DefaultLogger<GenerateEmbeddingsParallelHandler>.Instance;
        this._embeddingGenerationEnabled = orchestrator.EmbeddingGenerationEnabled;

        this._orchestrator = orchestrator;
        this._embeddingGenerators = orchestrator.GetEmbeddingGenerators();

        if (this._embeddingGenerationEnabled)
        {
            if (this._embeddingGenerators.Count < 1)
            {
                this._log.LogError("Handler '{0}' NOT ready, no embedding generators configured", stepName);
            }

            this._log.LogInformation("Handler '{0}' ready, {1} embedding generators", stepName, this._embeddingGenerators.Count);
        }
        else
        {
            this._log.LogInformation("Handler '{0}' ready, embedding generation DISABLED", stepName);
        }
    }

    /// <inheritdoc />
    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        if (!this._embeddingGenerationEnabled)
        {
            this._log.LogTrace("Embedding generation is disabled, skipping - pipeline '{0}/{1}'", pipeline.Index, pipeline.DocumentId);
            return (true, pipeline);
        }

        this._log.LogDebug("Generating embeddings, pipeline '{0}/{1}'", pipeline.Index, pipeline.DocumentId);

        var partitionsFound = false;
        foreach (var uploadedFile in pipeline.Files)
        {
            // Track new files being generated (cannot edit originalFile.GeneratedFiles while looping it)
            Dictionary<string, DataPipeline.GeneratedFileDetails> newFiles = new();

            var options = new ParallelOptions()
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            await Parallel.ForEachAsync(uploadedFile.GeneratedFiles, options, async (generatedFile, token) =>
            {
                var partitionFile = generatedFile.Value;
                if (partitionFile.AlreadyProcessedBy(this))
                {
                    partitionsFound = true;
                    this._log.LogTrace("File {0} already processed by this handler", partitionFile.Name);
                    return;
                }

                // Calc embeddings only for partitions (text chunks) and synthetic data
                if (partitionFile.ArtifactType != DataPipeline.ArtifactTypes.TextPartition && partitionFile.ArtifactType != DataPipeline.ArtifactTypes.SyntheticData)
                {
                    this._log.LogTrace("Skipping file {0} (not a partition, not synthetic data)", partitionFile.Name);
                    return;
                }

                partitionsFound = true;

                // TODO: cost/perf: if the partition SHA256 is the same and the embedding exists, avoid generating it again
                switch (partitionFile.MimeType)
                {
                    case MimeTypes.PlainText:
                    case MimeTypes.MarkDown:
                        this._log.LogTrace("Processing file {0}", partitionFile.Name);
                        foreach (ITextEmbeddingGenerator generator in this._embeddingGenerators)
                        {
                            EmbeddingFileContent embeddingData = new()
                            {
                                SourceFileName = partitionFile.Name
                            };

                            var generatorProviderClassName = generator.GetType().FullName ?? generator.GetType().Name;
                            embeddingData.GeneratorProvider = string.Join('.', generatorProviderClassName.Split('.').TakeLast(3));

                            // TODO: model name
                            embeddingData.GeneratorName = "TODO";

                            this._log.LogTrace("Generating embeddings using {0}, file: {1}", embeddingData.GeneratorProvider, partitionFile.Name);

                            // Check if embeddings have already been generated
                            string embeddingFileName = GetEmbeddingFileName(partitionFile.Name, embeddingData.GeneratorProvider, embeddingData.GeneratorName);

                            // TODO: check if the file exists in storage
                            if (uploadedFile.GeneratedFiles.ContainsKey(embeddingFileName))
                            {
                                this._log.LogDebug("Embeddings for {0} have already been generated", partitionFile.Name);
                                continue;
                            }

                            // TODO: handle Azure.RequestFailedException - BlobNotFound
                            string partitionContent = await this._orchestrator.ReadTextFileAsync(pipeline, partitionFile.Name, token).ConfigureAwait(false);

                            var inputTokenCount = generator.CountTokens(partitionContent);
                            if (inputTokenCount > generator.MaxTokens)
                            {
                                this._log.LogWarning("The content size ({0} tokens) exceeds the embedding generator capacity ({1} max tokens)", inputTokenCount, generator.MaxTokens);
                            }

                            Embedding embedding = await generator.GenerateEmbeddingAsync(partitionContent, token).ConfigureAwait(false);
                            embeddingData.Vector = embedding;
                            embeddingData.VectorSize = embeddingData.Vector.Length;
                            embeddingData.TimeStamp = DateTimeOffset.UtcNow;

                            this._log.LogDebug("Saving embedding file {0}", embeddingFileName);
                            string text = JsonSerializer.Serialize(embeddingData);
                            await this._orchestrator.WriteTextFileAsync(pipeline, embeddingFileName, text, token).ConfigureAwait(false);

                            var embeddingFileNameDetails = new DataPipeline.GeneratedFileDetails
                            {
                                Id = Guid.NewGuid().ToString("N"),
                                ParentId = uploadedFile.Id,
                                SourcePartitionId = partitionFile.Id,
                                Name = embeddingFileName,
                                Size = text.Length,
                                MimeType = MimeTypes.TextEmbeddingVector,
                                ArtifactType = DataPipeline.ArtifactTypes.TextEmbeddingVector,
                                PartitionNumber = partitionFile.PartitionNumber,
                                SectionNumber = partitionFile.SectionNumber,
                                Tags = partitionFile.Tags,
                            };
                            embeddingFileNameDetails.MarkProcessedBy(this);

                            lock (newFiles)
                            {
                                newFiles.Add(embeddingFileName, embeddingFileNameDetails);
                            }
                        }

                        break;

                    default:
                        this._log.LogWarning("File {0} cannot be used to generate embedding, type not supported", partitionFile.Name);
                        return;
                }

                partitionFile.MarkProcessedBy(this);
            }).ConfigureAwait(false);

            // Add new files to pipeline status
            foreach (var file in newFiles)
            {
                uploadedFile.GeneratedFiles.Add(file.Key, file.Value);
            }
        }

        if (!partitionsFound)
        {
            this._log.LogWarning("Pipeline '{0}/{1}': text partitions not found, cannot generate embeddings, moving to next pipeline step.", pipeline.Index, pipeline.DocumentId);
        }

        return (true, pipeline);
    }

    private static string GetEmbeddingFileName(string srcFilename, string type, string embeddingName)
    {
        return $"{srcFilename}.{type}.{embeddingName}{FileExtensions.TextEmbeddingVector}";
    }
}
