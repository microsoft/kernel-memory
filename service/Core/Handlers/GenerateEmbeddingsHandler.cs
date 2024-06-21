// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Context;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.Handlers;

/// <summary>
/// Memory ingestion pipeline handler responsible for generating text embedding and saving them to the document storage.
/// </summary>
public sealed class GenerateEmbeddingsHandler : GenerateEmbeddingsHandlerBase, IPipelineStepHandler
{
    private readonly ILogger<GenerateEmbeddingsHandler> _log;
    private readonly List<ITextEmbeddingGenerator> _embeddingGenerators;
    private readonly bool _embeddingGenerationEnabled;

    /// <inheritdoc />
    public string StepName { get; }

    /// <summary>
    /// Handler responsible for generating embeddings and saving them to document storages (not memory db).
    /// Note: stepName and other params are injected with DI
    /// </summary>
    /// <param name="stepName">Pipeline step for which the handler will be invoked</param>
    /// <param name="orchestrator">Current orchestrator used by the pipeline, giving access to content and other helps.</param>
    /// <param name="loggerFactory">Application logger factory</param>
    public GenerateEmbeddingsHandler(
        string stepName,
        IPipelineOrchestrator orchestrator,
        ILoggerFactory? loggerFactory = null)
        : base(orchestrator, (loggerFactory ?? DefaultLogger.Factory).CreateLogger<GenerateEmbeddingsHandler>())
    {
        this.StepName = stepName;
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<GenerateEmbeddingsHandler>();
        this._embeddingGenerationEnabled = orchestrator.EmbeddingGenerationEnabled;
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

        foreach (ITextEmbeddingGenerator generator in this._embeddingGenerators)
        {
            var subStepName = GetSubStepName(generator);
            var partitions = await this.GetListOfPartitionsToProcessAsync(pipeline, subStepName, cancellationToken).ConfigureAwait(false);

            int batchSize = pipeline.GetContext().GetCustomEmbeddingGenerationBatchSizeOrDefault((generator as ITextEmbeddingBatchGenerator)?.MaxBatchSize ?? 1);
            if (batchSize > 1 && generator is ITextEmbeddingBatchGenerator batchGenerator)
            {
                await this.GenerateEmbeddingsWithBatchingAsync(pipeline, batchGenerator, batchSize, partitions, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await this.GenerateEmbeddingsOneAtATimeAsync(pipeline, generator, partitions, cancellationToken).ConfigureAwait(false);
            }
        }

        return (true, pipeline);
    }

    protected override IPipelineStepHandler ActualInstance => this;

    // Generate and save embeddings, one batch at a time
    private async Task GenerateEmbeddingsWithBatchingAsync(
        DataPipeline pipeline,
        ITextEmbeddingBatchGenerator generator,
        int batchSize,
        List<PartitionInfo> partitions,
        CancellationToken cancellationToken)
    {
        PartitionInfo[][] batches = partitions.Chunk(batchSize).ToArray();

        this._log.LogTrace("Generating embeddings, pipeline '{0}/{1}', batch generator '{2}', batch size {3}, batch count {4}",
            pipeline.Index, pipeline.DocumentId, generator.GetType().FullName, generator.MaxBatchSize, batches.Length);

        // One batch at a time
        foreach (PartitionInfo[] partitionsInfo in batches)
        {
            string[] strings = partitionsInfo.Select(x => x.PartitionContent).ToArray();

            int totalTokens = strings.Sum(s => ((ITextEmbeddingGenerator)generator).CountTokens(s));
            this._log.LogTrace("Generating embeddings, pipeline '{0}/{1}', generator '{2}', batch size {3}, total {4} tokens",
                pipeline.Index, pipeline.DocumentId, generator.GetType().FullName, strings.Length, totalTokens);

            Embedding[] embeddings = await generator.GenerateEmbeddingBatchAsync(strings, cancellationToken).ConfigureAwait(false);
            await this.SaveEmbeddingsToDocumentStorageAsync(
                    pipeline, partitionsInfo, embeddings, GetEmbeddingProviderName(generator), GetEmbeddingGeneratorName(generator), cancellationToken)
                .ConfigureAwait(false);
        }
    }

    // Generate and save embeddings, one chunk at a time
    private async Task GenerateEmbeddingsOneAtATimeAsync(
        DataPipeline pipeline,
        ITextEmbeddingGenerator generator,
        List<PartitionInfo> partitions,
        CancellationToken cancellationToken)
    {
        this._log.LogTrace("Generating embeddings, pipeline '{0}/{1}', generator '{2}', partition count {3}",
            pipeline.Index, pipeline.DocumentId, generator.GetType().FullName, partitions.Count);

        // One partition at a time
        foreach (PartitionInfo partitionInfo in partitions)
        {
            this._log.LogTrace("Generating embedding, pipeline '{0}/{1}', generator '{2}', content size {3} tokens",
                pipeline.Index, pipeline.DocumentId, generator.GetType().FullName, generator.CountTokens(partitionInfo.PartitionContent));
            var embedding = await generator.GenerateEmbeddingAsync(partitionInfo.PartitionContent, cancellationToken).ConfigureAwait(false);
            await this.SaveEmbeddingToDocumentStorageAsync(
                    pipeline, partitionInfo, embedding, GetEmbeddingProviderName(generator), GetEmbeddingGeneratorName(generator), cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
