// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search;

/// <summary>
/// Base interface for all search index types.
/// Implementations include FTS, vector search, graph search, etc.
/// </summary>
public interface ISearchIndex
{
    /// <summary>
    /// Updates this index when content is created or updated.
    /// </summary>
    /// <param name="contentId">The content ID to index.</param>
    /// <param name="text">The text content to index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task IndexAsync(string contentId, string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes content from this index.
    /// Idempotent - no error if content doesn't exist.
    /// </summary>
    /// <param name="contentId">The content ID to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveAsync(string contentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all entries from this index.
    /// Used for rebuilding from content storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
