// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel.Embeddings;

namespace Microsoft.SemanticKernel.AI.Embeddings;

/// <summary>
/// Extension methods for ITextEmbeddingGeneration
/// </summary>
public static partial class TextEmbeddingGenerationExtensions
{
    /// <summary>
    /// Generate the embedding vector for a single string
    /// </summary>
    /// <param name="generator">Embedding generator</param>
    /// <param name="text">Text to calculate the embedding for</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Embedding vector</returns>
    public static async Task<Embedding> GenerateEmbeddingAsync(
        this ITextEmbeddingGenerationService generator, string text, CancellationToken cancellationToken = default)
    {
        IList<ReadOnlyMemory<float>>? embeddings = await generator
            .GenerateEmbeddingsAsync([text], null, cancellationToken)
            .ConfigureAwait(false);
        if (embeddings.Count == 0)
        {
            throw new KernelMemoryException("Failed to generate embedding for the given text");
        }

        return embeddings.First();
    }
}
