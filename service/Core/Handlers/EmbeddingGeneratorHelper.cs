// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.Handlers;

/// <summary>
/// Allows a unique and optimzied way to generate a cache of embedding for a given
/// text and a list of embedding generators.
/// </summary>
internal class EmbeddingGeneratorHelper
{
    private readonly List<ITextEmbeddingGenerator> _embeddingGenerators;
    private readonly Dictionary<ITextEmbeddingGenerator, Dictionary<string, Embedding>> _cache;

    public static async Task<EmbeddingGeneratorHelper> GetEmbeddingsAsync(
        DataPipeline pipeline,
        IPipelineStepHandler pipelineStepHandler,
        List<ITextEmbeddingGenerator> embeddingGenerators,
        IPipelineOrchestrator orchestrator,
        CancellationToken cancellationToken = default)
    {
        EmbeddingGeneratorHelper helper = new(embeddingGenerators);
        List<string> textSnippets = new();
        foreach (var uploadedFile in pipeline.Files)
        {
            foreach (KeyValuePair<string, DataPipeline.GeneratedFileDetails> generatedFile in uploadedFile.GeneratedFiles)
            {
                // Following code is suboptimal because it needs to mimic the code of the GenerateEmbeddingHandler that uses some of these
                // Condition to avoid generating the partition embedding.
                var partitionFile = generatedFile.Value;

                if (partitionFile.AlreadyProcessedBy(pipelineStepHandler))
                {
                    continue;
                }

                // Calc embeddings only for partitions (text chunks) and synthetic data
                if (partitionFile.ArtifactType != DataPipeline.ArtifactTypes.TextPartition
                    && partitionFile.ArtifactType != DataPipeline.ArtifactTypes.SyntheticData)
                {
                    continue;
                }

                // only some of the mime type can be used to generate embeddings.
                switch (partitionFile.MimeType)
                {
                    case MimeTypes.PlainText:
                    case MimeTypes.MarkDown:
                        string partitionContent = await orchestrator.ReadTextFileAsync(pipeline, partitionFile.Name, cancellationToken).ConfigureAwait(false);
                        textSnippets.Add(partitionContent);
                        break;
                }
            }
        }

        if (textSnippets.Count > 0)
        {
            await helper.GenerateEmbeddingAsync(textSnippets, cancellationToken).ConfigureAwait(false);
        }

        return helper;
    }

    public EmbeddingGeneratorHelper(List<ITextEmbeddingGenerator> embeddingGenerators)
    {
        this._embeddingGenerators = embeddingGenerators ?? throw new System.ArgumentNullException(nameof(embeddingGenerators));
        this._cache = new();
        foreach (var embeddingGenerator in embeddingGenerators)
        {
            this._cache[embeddingGenerator] = new();
        }
    }

    public async Task GenerateEmbeddingAsync(IList<string> text, CancellationToken cancellationToken)
    {
        //we will proceed for each generator
        //we will first check if we can use the batch generation
        foreach (var generator in this._embeddingGenerators)
        {
            //ok now we can calculate the embedding in batch if we have more than one text and batch generator is active.
            if (text.Count > 1 && generator is ITextEmbeddingBatchGenerator batchGenerator)
            {
                List<string> batch = new();
                int tokenCount = 0;
                for (int i = 0; i < text.Count; i++)
                {
                    var singlePieceOfText = text[i];
                    var pieceTokenCount = generator.CountTokens(singlePieceOfText);

                    // The next piece of element will exceed the limit, or we reached maximum batch size.
                    if (BatchReachedMaximumNumberOfElements(generator, batchGenerator, batch, tokenCount, pieceTokenCount))
                    {
                        await this.BatchConvertAsync(generator, batchGenerator, batch, cancellationToken).ConfigureAwait(false);
                        tokenCount = 0; //a new token count starts.
                    }

                    batch.Add(singlePieceOfText);
                    tokenCount += pieceTokenCount;
                }

                //flush the last batch.
                if (batch.Count > 0)
                {
                    await this.BatchConvertAsync(generator, batchGenerator, batch, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                //sequentially generate the embeddings, no way to optimize.
                foreach (var singlePieceOfText in text)
                {
                    Embedding embedding = await generator.GenerateEmbeddingAsync(singlePieceOfText, cancellationToken).ConfigureAwait(false);
                    this._cache[generator][singlePieceOfText] = embedding;
                }
            }
        }
    }

    private static bool BatchReachedMaximumNumberOfElements(
        ITextEmbeddingGenerator generator,
        ITextEmbeddingBatchGenerator batchGenerator,
        List<string> batch,
        int tokenCount,
        int pieceTokenCount)
    {
        return tokenCount + pieceTokenCount > generator.MaxTokens || batch.Count == batchGenerator.MaxBatchSize;
    }

    private async Task BatchConvertAsync(
        ITextEmbeddingGenerator generator,
        ITextEmbeddingBatchGenerator batchGenerator,
        List<string> batch,
        CancellationToken cancellationToken)
    {
        //we have enough text to generate the embeddings
        IReadOnlyList<Embedding> embeddings = await batchGenerator.GenerateEmbeddingBatchAsync(batch, cancellationToken).ConfigureAwait(false);
        for (int j = 0; j < batch.Count; j++)
        {
            this._cache[generator][batch[j]] = embeddings[j];
        }

        batch.Clear();
    }

    public Embedding GetEmbedding(ITextEmbeddingGenerator embeddingGenerator, string text)
    {
        return this._cache[embeddingGenerator][text];
    }
}
