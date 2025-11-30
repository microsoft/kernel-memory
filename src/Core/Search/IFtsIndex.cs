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
    /// Searches the full-text index for matching content.
    /// </summary>
    /// <param name="query">The search query (FTS5 syntax supported).</param>
    /// <param name="limit">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matches ordered by relevance (highest score first).</returns>
    Task<IReadOnlyList<FtsMatch>> SearchAsync(string query, int limit = 10, CancellationToken cancellationToken = default);
}
