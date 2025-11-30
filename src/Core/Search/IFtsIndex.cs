// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search;

/// <summary>
/// Interface for Full-Text Search index operations.
/// Extends ISearchIndex with search-specific capabilities.
/// Implementations include SQLite FTS5 and PostgreSQL FTS.
/// </summary>
public interface IFtsIndex : ISearchIndex
{
    /// <summary>
    /// Indexes content with separate FTS-indexed fields.
    /// BREAKING CHANGE: New signature to support title, description, content separately.
    /// </summary>
    /// <param name="contentId">Unique content identifier.</param>
    /// <param name="title">Optional title (FTS-indexed).</param>
    /// <param name="description">Optional description (FTS-indexed).</param>
    /// <param name="content">Main content body (FTS-indexed, required).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task IndexAsync(string contentId, string? title, string? description, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches the full-text index for matching content.
    /// </summary>
    /// <param name="query">The search query (FTS5 syntax supported).</param>
    /// <param name="limit">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matches ordered by relevance (highest score first).</returns>
    Task<IReadOnlyList<FtsMatch>> SearchAsync(string query, int limit = 10, CancellationToken cancellationToken = default);
}
