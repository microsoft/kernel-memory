// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Config.Enums;

namespace KernelMemory.Core.Embeddings.Cache;

/// <summary>
/// Interface for embedding cache implementations.
/// Supports dependency injection and multiple cache implementations (SQLite, etc.).
/// </summary>
public interface IEmbeddingCache
{
    /// <summary>
    /// Cache mode (read-write, read-only, write-only).
    /// Controls whether read and write operations are allowed.
    /// </summary>
    CacheModes Mode { get; }

    /// <summary>
    /// Try to retrieve a cached embedding by key.
    /// Returns null if not found or if mode is WriteOnly.
    /// </summary>
    /// <param name="key">The cache key to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The cached embedding if found, null otherwise.</returns>
    Task<CachedEmbedding?> TryGetAsync(EmbeddingCacheKey key, CancellationToken ct = default);

    /// <summary>
    /// Store an embedding in the cache with optional token count.
    /// Does nothing if mode is ReadOnly.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="vector">The embedding vector to store.</param>
    /// <param name="tokenCount">Optional token count if provider reports it.</param>
    /// <param name="ct">Cancellation token.</param>
    Task StoreAsync(EmbeddingCacheKey key, float[] vector, int? tokenCount, CancellationToken ct = default);
}
