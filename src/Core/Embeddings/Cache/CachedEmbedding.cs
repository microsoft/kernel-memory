// Copyright (c) Microsoft. All rights reserved.
using System.Diagnostics.CodeAnalysis;

namespace KernelMemory.Core.Embeddings.Cache;

/// <summary>
/// Represents a cached embedding vector with metadata.
/// Stores the vector, optional token count, and timestamp of when it was cached.
/// </summary>
public sealed class CachedEmbedding
{
    /// <summary>
    /// The embedding vector as a float array.
    /// Array is intentional for performance - embeddings are read-only after creation.
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays",
        Justification = "Embedding vectors are performance-critical and read-only after creation")]
    public required float[] Vector { get; init; }

    /// <summary>
    /// Optional token count from the provider response.
    /// Null if the provider did not return token count.
    /// </summary>
    public int? TokenCount { get; init; }

    /// <summary>
    /// Timestamp when this embedding was stored in the cache.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }
}
