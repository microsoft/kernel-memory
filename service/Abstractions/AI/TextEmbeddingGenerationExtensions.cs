// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.AI.Embeddings;

namespace Microsoft.KernelMemory.AI;

public static class TextEmbeddingGenerationExtensions
{
    public static async Task<Embedding> GenerateEmbeddingAsync(
        this ITextEmbeddingGeneration generator, string text, CancellationToken cancellationToken = default)
    {
        var embeddings = await generator
            .GenerateEmbeddingsAsync(new List<string> { text }, cancellationToken)
            .ConfigureAwait(false);
        if (embeddings.Count == 0)
        {
            throw new KernelMemoryException("Failed to generate embedding for the given text");
        }

        return embeddings.First();
    }
}
