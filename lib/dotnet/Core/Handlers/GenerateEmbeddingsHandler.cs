// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticKernel.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticKernel.SemanticMemory.Core.Configuration;
using Microsoft.SemanticKernel.SemanticMemory.Core.Diagnostics;
using Microsoft.SemanticKernel.SemanticMemory.Core.Pipeline;

namespace Microsoft.SemanticKernel.SemanticMemory.Core.Handlers;

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
        SKMemoryConfig configuration,
        ILogger<GenerateEmbeddingsHandler>? log = null)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;
        this._log = log ?? NullLogger<GenerateEmbeddingsHandler>.Instance;
        this._embeddingGenerators = new List<object>();

        var handlerConfig = configuration.GetHandlerConfig<EmbeddingGenerationConfig>(stepName);
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

        foreach (var originalFile in pipeline.Files)
        {
            // Track new files being generated (cannot edit originalFile.GeneratedFiles while looping it)
            Dictionary<string, DataPipeline.GeneratedFileDetails> newFiles = new();

            foreach (KeyValuePair<string, DataPipeline.GeneratedFileDetails> generatedFile in originalFile.GeneratedFiles)
            {
                var file = generatedFile.Value;

                // Calc embeddings only for partitions
                if (!file.IsPartition)
                {
                    this._log.LogTrace("Skipping file {0} (not a partition)", file.Name);
                    continue;
                }

                switch (file.Type)
                {
                    case MimeTypes.PlainText:
                    case MimeTypes.MarkDown:
                        this._log.LogTrace("Processing file {0}", file.Name);
                        foreach (object cfg in this._embeddingGenerators)
                        {
                            Embedding<float> vector = new();
                            string embeddingFileName;

                            switch (cfg)
                            {
                                case AzureOpenAIConfig x:
                                {
                                    this._log.LogTrace("Generating embeddings using Azure OpenAI, file: {0}", file.Name);

                                    // Check if embeddings have already been generated
                                    embeddingFileName = GetEmbeddingFileName(file.Name, "AzureOpenAI", x.Deployment);
                                    if (originalFile.GeneratedFiles.ContainsKey(embeddingFileName))
                                    {
                                        this._log.LogDebug("Embeddings for {0} have already been generated", file.Name);
                                        continue;
                                    }

                                    // TODO: handle Azure.RequestFailedException - BlobNotFound
                                    string content = await this._orchestrator.ReadTextFileAsync(pipeline, file.Name, cancellationToken).ConfigureAwait(false);

                                    var generator = new AzureTextEmbeddingGeneration(
                                        modelId: x.Deployment, endpoint: x.Endpoint, apiKey: x.APIKey, logger: this._log);

                                    IList<Embedding<float>> embedding = await generator.GenerateEmbeddingsAsync(
                                        new List<string> { content }, cancellationToken).ConfigureAwait(false);

                                    if (embedding.Count == 0)
                                    {
                                        throw new OrchestrationException("Embeddings not generated");
                                    }

                                    vector = embedding.First();
                                    break;
                                }

                                case OpenAIConfig x:
                                {
                                    this._log.LogTrace("Generating embeddings using OpenAI, file: {0}", file.Name);

                                    // Check if embeddings have already been generated
                                    embeddingFileName = GetEmbeddingFileName(file.Name, "OpenAI", x.Model);
                                    if (originalFile.GeneratedFiles.ContainsKey(embeddingFileName))
                                    {
                                        this._log.LogDebug("Embeddings for {0} have already been generated", file.Name);
                                        continue;
                                    }

                                    var generator = new OpenAITextEmbeddingGeneration(
                                        modelId: x.Model, apiKey: x.APIKey, organization: x.OrgId);
                                    string content = await this._orchestrator.ReadTextFileAsync(pipeline, file.Name, cancellationToken).ConfigureAwait(false);

                                    IList<Embedding<float>> embedding = await generator.GenerateEmbeddingsAsync(
                                        new List<string> { content }, cancellationToken).ConfigureAwait(false);

                                    if (embedding.Count == 0)
                                    {
                                        throw new OrchestrationException("Embeddings not generated");
                                    }

                                    vector = embedding.First();
                                    break;
                                }

                                default:
                                    this._log.LogError("Embedding generator {0} not supported", cfg.GetType().FullName);
                                    throw new OrchestrationException($"Embeddings generator {cfg.GetType().FullName} not supported");
                            }

                            this._log.LogDebug("Saving embedding file {0}", embeddingFileName);
                            var text = JsonSerializer.Serialize(vector);
                            await this._orchestrator.WriteTextFileAsync(pipeline, embeddingFileName, text, cancellationToken).ConfigureAwait(false);

                            newFiles.Add(embeddingFileName, new DataPipeline.GeneratedFileDetails
                            {
                                Name = embeddingFileName,
                                Size = text.Length,
                                Type = MimeTypes.TextEmbeddingVector,
                                IsPartition = false
                            });
                        }

                        break;

                    default:
                        this._log.LogWarning("File {0} cannot be used to generate embedding, type not supported", file.Name);
                        continue;
                }
            }

            // Add new files to pipeline status
            foreach (var file in newFiles)
            {
                originalFile.GeneratedFiles.Add(file.Key, file.Value);
            }
        }

        return (true, pipeline);
    }

    private static string GetEmbeddingFileName(string srcFilename, string type, string embeddingName)
    {
        return $"{srcFilename}.{type}.{embeddingName}{FileExtensions.TextEmbeddingVector}";
    }
}
