﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticMemory.ContentStorage;
using Microsoft.SemanticMemory.Diagnostics;
using Microsoft.SemanticMemory.Pipeline;

namespace Microsoft.SemanticMemory.Handlers;

/// <summary>
/// Memory ingestion pipeline handler responsible for generating text embedding and saving them to the content storage.
/// </summary>
public class GenerateEmbeddingsHandler : IPipelineStepHandler
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly List<ITextEmbeddingGeneration> _embeddingGenerators;
    private readonly ILogger<GenerateEmbeddingsHandler> _log;

    /// <inheritdoc />
    public string StepName { get; }

    /// <summary>
    /// Handler responsible for generating embeddings and saving them to content storages.
    /// Note: stepName and other params are injected with DI
    /// </summary>
    /// <param name="stepName">Pipeline step for which the handler will be invoked</param>
    /// <param name="orchestrator">Current orchestrator used by the pipeline, giving access to content and other helps.</param>
    /// <param name="log">Application logger</param>
    public GenerateEmbeddingsHandler(
        string stepName,
        IPipelineOrchestrator orchestrator,
        ILogger<GenerateEmbeddingsHandler>? log = null)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;
        this._log = log ?? DefaultLogger<GenerateEmbeddingsHandler>.Instance;
        this._embeddingGenerators = orchestrator.GetEmbeddingGenerators();

        this._log.LogInformation("Handler '{0}' ready, {1} embedding generators", stepName, this._embeddingGenerators.Count);
        if (this._embeddingGenerators.Count < 1)
        {
            this._log.LogError("No embedding generators configured");
        }
    }

    /// <inheritdoc />
    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        this._log.LogTrace("Generating embeddings, pipeline {0}", pipeline.DocumentId);

        foreach (var uploadedFile in pipeline.Files)
        {
            // Track new files being generated (cannot edit originalFile.GeneratedFiles while looping it)
            Dictionary<string, DataPipeline.GeneratedFileDetails> newFiles = new();

            foreach (KeyValuePair<string, DataPipeline.GeneratedFileDetails> generatedFile in uploadedFile.GeneratedFiles)
            {
                var partitionFile = generatedFile.Value;
                if (partitionFile.AlreadyProcessedBy(this))
                {
                    this._log.LogTrace("File {0} already processed by this handler", partitionFile.Name);
                    continue;
                }

                // Calc embeddings only for partitions (text chunks) and synthetic data
                if (partitionFile.ArtifactType != DataPipeline.ArtifactTypes.TextPartition
                    && partitionFile.ArtifactType != DataPipeline.ArtifactTypes.SyntheticData)
                {
                    this._log.LogTrace("Skipping file {0} (not a partition, not synthetic data)", partitionFile.Name);
                    continue;
                }

                // TODO: cost/perf: if the partition SHA256 is the same and the embedding exists, avoid generating it again
                switch (partitionFile.MimeType)
                {
                    case MimeTypes.PlainText:
                    case MimeTypes.MarkDown:
                        this._log.LogTrace("Processing file {0}", partitionFile.Name);
                        foreach (ITextEmbeddingGeneration generator in this._embeddingGenerators)
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
                            string partitionContent = await this._orchestrator.ReadTextFileAsync(pipeline, partitionFile.Name, cancellationToken).ConfigureAwait(false);

                            IList<ReadOnlyMemory<float>> embedding = await generator.GenerateEmbeddingsAsync(
                                new List<string> { partitionContent }, cancellationToken).ConfigureAwait(false);

                            if (embedding.Count == 0)
                            {
                                throw new OrchestrationException("Embeddings not generated");
                            }

                            embeddingData.Vector = embedding.First();
                            embeddingData.VectorSize = embeddingData.Vector.Length;
                            embeddingData.TimeStamp = DateTimeOffset.UtcNow;

                            this._log.LogDebug("Saving embedding file {0}", embeddingFileName);
                            string text = JsonSerializer.Serialize(embeddingData);
                            await this._orchestrator.WriteTextFileAsync(pipeline, embeddingFileName, text, cancellationToken).ConfigureAwait(false);

                            var embeddingFileNameDetails = new DataPipeline.GeneratedFileDetails
                            {
                                Id = Guid.NewGuid().ToString("N"),
                                ParentId = uploadedFile.Id,
                                Name = embeddingFileName,
                                Size = text.Length,
                                MimeType = MimeTypes.TextEmbeddingVector,
                                ArtifactType = DataPipeline.ArtifactTypes.TextEmbeddingVector
                            };
                            embeddingFileNameDetails.MarkProcessedBy(this);
                            newFiles.Add(embeddingFileName, embeddingFileNameDetails);
                        }

                        break;

                    default:
                        this._log.LogWarning("File {0} cannot be used to generate embedding, type not supported", partitionFile.Name);
                        continue;
                }

                partitionFile.MarkProcessedBy(this);
            }

            // Add new files to pipeline status
            foreach (var file in newFiles)
            {
                uploadedFile.GeneratedFiles.Add(file.Key, file.Value);
            }
        }

        return (true, pipeline);
    }

    private static string GetEmbeddingFileName(string srcFilename, string type, string embeddingName)
    {
        return $"{srcFilename}.{type}.{embeddingName}{FileExtensions.TextEmbeddingVector}";
    }
}
