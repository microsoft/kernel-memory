// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search.Models;

/// <summary>
/// Response from a search operation.
/// Contains results, metadata, and telemetry.
/// </summary>
public sealed class SearchResponse
{
    /// <summary>
    /// The original query string that was executed.
    /// </summary>
    public required string Query { get; init; }

    /// <summary>
    /// Total number of results returned (after filtering and pagination).
    /// </summary>
    public required int TotalResults { get; init; }

    /// <summary>
    /// Search results ordered by relevance (DESC) then createdAt (DESC).
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
    public required SearchResult[] Results { get; init; }

    /// <summary>
    /// Metadata about search execution (timing, warnings, etc.).
    /// </summary>
    public required SearchMetadata Metadata { get; init; }
}
