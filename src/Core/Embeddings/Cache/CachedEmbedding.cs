// Copyright (c) Microsoft. All rights reserved.
using System.Diagnostics.CodeAnalysis;

namespace KernelMemory.Core.Embeddings.Cache;

/// <summary>
/// Represents a cached embedding vector.
/// </summary>
public sealed class CachedEmbedding
{
    /// <summary>
    /// The embedding vector.
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays",
        Justification = "Embedding vectors are read-only after creation and passed to storage layer")]
    public required float[] Vector { get; init; }
}
