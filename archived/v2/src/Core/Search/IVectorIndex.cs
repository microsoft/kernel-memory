// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search;

/// <summary>
/// Interface for vector search index operations.
/// Extends ISearchIndex with vector-specific capabilities.
/// All vectors are normalized at write time, searches use dot product (equivalent to cosine similarity).
/// </summary>
public interface IVectorIndex : ISearchIndex
{
    /// <summary>
    /// Vector dimensions for this index (must match embedding model).
    /// </summary>
    int VectorDimensions { get; }

    /// <summary>
    /// Indexes content with vector embedding.
    /// Generates embedding using configured generator, normalizes it, then stores.
    /// </summary>
    /// <param name="contentId">Unique content identifier.</param>
    /// <param name="text">Text to generate embedding for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    new Task IndexAsync(string contentId, string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches the vector index for similar content using dot product on normalized vectors.
    /// </summary>
    /// <param name="queryText">Query text to generate embedding for.</param>
    /// <param name="limit">Maximum number of results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matches ordered by similarity (highest score first).</returns>
    Task<IReadOnlyList<VectorMatch>> SearchAsync(string queryText, int limit = 10, CancellationToken cancellationToken = default);
}
