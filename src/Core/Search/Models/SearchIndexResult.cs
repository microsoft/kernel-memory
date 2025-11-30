// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search.Models;

/// <summary>
/// Raw search result from a single index before reranking.
/// Multiple SearchIndexResults can refer to the same record (different indexes or chunks).
/// This is an internal model used by the reranking algorithm.
/// </summary>
public sealed class SearchIndexResult
{
    // Identity

    /// <summary>
    /// Record identifier.
    /// </summary>
    public required string RecordId { get; init; }

    /// <summary>
    /// Node ID where this result originated.
    /// </summary>
    public required string NodeId { get; init; }

    /// <summary>
    /// Index ID (e.g., "fts-main", "vector-primary").
    /// </summary>
    public required string IndexId { get; init; }

    /// <summary>
    /// Optional chunk ID if this is a chunk of a larger document.
    /// Used when same record appears multiple times from same index.
    /// </summary>
    public string? ChunkId { get; init; }

    // Scoring

    /// <summary>
    /// Raw score from index (0.0-1.0) before weight application.
    /// </summary>
    public required float BaseRelevance { get; init; }

    // Full record data (needed for highlighting, snippets, and final output)

    // FTS-indexed fields

    /// <summary>
    /// Optional title (FTS-indexed).
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Optional description (FTS-indexed).
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Main content (FTS-indexed).
    /// </summary>
    public required string Content { get; init; }

    // Filter-only fields (NOT FTS-indexed)

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// MIME type.
    /// </summary>
    public string MimeType { get; init; } = string.Empty;

    /// <summary>
    /// Tags.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
    public string[] Tags { get; init; } = [];

    /// <summary>
    /// Metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}
