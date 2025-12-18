// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;

namespace KernelMemory.Core.Embeddings;

/// <summary>
/// Result of embedding generation including the vector and optional metadata.
/// </summary>
public sealed class EmbeddingResult
{
    /// <summary>
    /// The generated embedding vector.
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays",
        Justification = "Embedding vectors are read-only after creation")]
    public required float[] Vector { get; init; }

    /// <summary>
    /// Optional token count if the provider reports it.
    /// Used for cost tracking and usage monitoring.
    /// </summary>
    public int? TokenCount { get; init; }

    /// <summary>
    /// Creates an EmbeddingResult with just a vector (no token count).
    /// </summary>
    public static EmbeddingResult FromVector(float[] vector)
    {
        return new EmbeddingResult { Vector = vector, TokenCount = null };
    }

    /// <summary>
    /// Creates an EmbeddingResult with vector and token count.
    /// </summary>
    public static EmbeddingResult FromVectorWithTokens(float[] vector, int tokenCount)
    {
        return new EmbeddingResult { Vector = vector, TokenCount = tokenCount };
    }
}
