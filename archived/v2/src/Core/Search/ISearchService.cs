// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Search.Models;

namespace KernelMemory.Core.Search;

/// <summary>
/// Service interface for searching across nodes and indexes.
/// Transport-agnostic - used by CLI, Web API, and RPC.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Execute a search query across configured nodes and indexes.
    /// Supports both infix notation and MongoDB JSON query formats.
    /// </summary>
    /// <param name="request">The search request with query and options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results with metadata.</returns>
    Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate a query without executing it.
    /// Returns validation result with detailed errors if invalid.
    /// Useful for UI builders, debugging, and LLM query generation validation.
    /// </summary>
    /// <param name="query">The query string to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    Task<QueryValidationResult> ValidateQueryAsync(string query, CancellationToken cancellationToken = default);
}
