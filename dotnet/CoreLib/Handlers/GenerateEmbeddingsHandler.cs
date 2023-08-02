// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.ContentStorage;
using Microsoft.SemanticMemory.Core.Diagnostics;
using Microsoft.SemanticMemory.Core.Pipeline;

namespace Microsoft.SemanticMemory.Core.Handlers;

/// <summary>
/// Memory ingestion pipeline handler responsible for generating text embedding and saving them to the content storage.
/// </summary>
public class GenerateEmbeddingsHandler : IPipelineStepHandler
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly List<object> _embeddingGenerators;
    private readonly ILogger<GenerateEmbeddingsHandler> _log;

    /// <summary>
    /// Note: stepName and other params are injected with DI, <see cref="DependencyInjection.UseHandler{THandler}"/>
    /// </summary>
    /// <param name="stepName">Pipeline step for which the handler will be invoked</param>
    /// <param name="orchestrator">Current orchestrator used by the pipeline, giving access to content and other helps.</param>
    /// <param name="configuration">Application settings</param>
    /// <param name="log">Application logger</param>
    public GenerateEmbeddingsHandler(
        string stepName,
        IPipelineOrchestrator orchestrator,
        SemanticMemoryConfig configuration,
        ILogger<GenerateEmbeddingsHandler>? log = null)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;
        this._log = log ?? DefaultLogger<GenerateEmbeddingsHandler>.Instance;
        this._embeddingGenerators = new List<object>();

        var handlerConfig = configuration.GetHandlerConfig<EmbeddingGeneratorsConfig>(stepName);
        for (int index = 0; index < handlerConfig.EmbeddingGenerators.Count; index++)
        {
            this._embeddingGenerators.Add(handlerConfig.GetEmbeddingGeneratorConfig(index));
        }

        this._log.LogInformation("Handler '{0}' ready, {1} embedding generators", stepName, this._embeddingGenerators.Count);
        if (this._embeddingGenerators.Count < 1)
        {
            this._log.LogWarning("No embedding generators configured");
        }
    }

    /// <inheritdoc />
    public string StepName { get; }

    /// <inheritdoc />
    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken)
    {
        this._log.LogTrace("Generating embeddings, pipeline {0}", pipeline.Id);

        foreach (var uploadedFile in pipeline.Files)
        {
            // Track new files being generated (cannot edit originalFile.GeneratedFiles while looping it)
            Dictionary<string, DataPipeline.GeneratedFileDetails> newFiles = new();

            foreach (KeyValuePair<string, DataPipeline.GeneratedFileDetails> generatedFile in uploadedFile.GeneratedFiles)
            {
                var partitionFile = generatedFile.Value;

                // Calc embeddings only for partitions
                if (!partitionFile.IsPartition)
                {
                    this._log.LogTrace("Skipping file {0} (not a partition)", partitionFile.Name);
                    continue;
                }

                switch (partitionFile.Type)
                {
                    case MimeTypes.PlainText:
                    case MimeTypes.MarkDown:
                        this._log.LogTrace("Processing file {0}", partitionFile.Name);
                        foreach (object cfg in this._embeddingGenerators)
                        {
                            EmbeddingFileContent embeddingData = new()
                            {
                                SourceFileName = partitionFile.Name
                            };

                            string embeddingFileName;

                            switch (cfg)
                            {
                                case AzureOpenAIConfig x:
                                {
                                    embeddingData.GeneratorProvider = "AzureOpenAI";
                                    // TODO: fetch the model name
                                    embeddingData.GeneratorName = x.Deployment;

                                    this._log.LogTrace("Generating embeddings using Azure OpenAI, file: {0}", partitionFile.Name);

                                    // Check if embeddings have already been generated
                                    embeddingFileName = GetEmbeddingFileName(partitionFile.Name, "AzureOpenAI", x.Deployment);
                                    if (uploadedFile.GeneratedFiles.ContainsKey(embeddingFileName))
                                    {
                                        this._log.LogDebug("Embeddings for {0} have already been generated", partitionFile.Name);
                                        continue;
                                    }

                                    // TODO: handle Azure.RequestFailedException - BlobNotFound
                                    string partitionContent = await this._orchestrator.ReadTextFileAsync(pipeline, partitionFile.Name, cancellationToken).ConfigureAwait(false);

                                    var generator = new AzureTextEmbeddingGeneration(
                                        modelId: x.Deployment, endpoint: x.Endpoint, apiKey: x.APIKey, logger: this._log);

                                    IList<Embedding<float>> embedding = await generator.GenerateEmbeddingsAsync(
                                        new List<string> { partitionContent }, cancellationToken).ConfigureAwait(false);

                                    if (embedding.Count == 0)
                                    {
                                        throw new OrchestrationException("Embeddings not generated");
                                    }

                                    embeddingData.Vector = embedding.First();
                                    break;
                                }

                                case OpenAIConfig x:
                                {
                                    embeddingData.GeneratorProvider = "OpenAI";
                                    embeddingData.GeneratorName = x.Model;

                                    this._log.LogTrace("Generating embeddings using OpenAI, file: {0}", partitionFile.Name);

                                    // Check if embeddings have already been generated
                                    embeddingFileName = GetEmbeddingFileName(partitionFile.Name, "OpenAI", x.Model);
                                    if (uploadedFile.GeneratedFiles.ContainsKey(embeddingFileName))
                                    {
                                        this._log.LogDebug("Embeddings for {0} have already been generated", partitionFile.Name);
                                        continue;
                                    }

                                    var generator = new OpenAITextEmbeddingGeneration(
                                        modelId: x.Model, apiKey: x.APIKey, organization: x.OrgId, logger: this._log);
                                    string content = await this._orchestrator.ReadTextFileAsync(pipeline, partitionFile.Name, cancellationToken).ConfigureAwait(false);

                                    IList<Embedding<float>> embedding = await generator.GenerateEmbeddingsAsync(
                                        new List<string> { content }, cancellationToken).ConfigureAwait(false);

                                    if (embedding.Count == 0)
                                    {
                                        throw new OrchestrationException("Embeddings not generated");
                                    }

                                    embeddingData.Vector = embedding.First();
                                    break;
                                }

                                default:
                                    this._log.LogError("Embedding generator {0} not supported", cfg.GetType().FullName);
                                    throw new OrchestrationException($"Embeddings generator {cfg.GetType().FullName} not supported");
                            }

                            embeddingData.VectorSize = embeddingData.Vector.Count;
                            embeddingData.TimeStamp = DateTimeOffset.UtcNow;

                            this._log.LogDebug("Saving embedding file {0}", embeddingFileName);
                            string text = JsonSerializer.Serialize(embeddingData);
                            await this._orchestrator.WriteTextFileAsync(pipeline, embeddingFileName, text, cancellationToken).ConfigureAwait(false);

                            newFiles.Add(embeddingFileName, new DataPipeline.GeneratedFileDetails
                            {
                                Id = Guid.NewGuid().ToString("N"),
                                ParentId = uploadedFile.Id,
                                Name = embeddingFileName,
                                Size = text.Length,
                                Type = MimeTypes.TextEmbeddingVector,
                                IsPartition = false
                            });
                        }

                        break;

                    default:
                        this._log.LogWarning("File {0} cannot be used to generate embedding, type not supported", partitionFile.Name);
                        continue;
                }
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
