// Copyright (c) Microsoft. All rights reserved.
using System.Diagnostics.CodeAnalysis;

namespace KernelMemory.Core.Embeddings.Cache;

/// <summary>
/// Represents a cached embedding vector with metadata.
/// </summary>
public sealed class CachedEmbedding
{
    /// <summary>
    /// The embedding vector.
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays",
        Justification = "Embedding vectors are read-only after creation and passed to storage layer")]
    public required float[] Vector { get; init; }

    /// <summary>
    /// Optional token count returned by the provider.
    /// Null if provider doesn't report token usage.
    /// </summary>
    public int? TokenCount { get; init; }

    /// <summary>
    /// When this cache entry was created.
    /// Used for debugging and potential future cache eviction.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }
}
