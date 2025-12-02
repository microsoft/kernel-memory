// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search.Models;

/// <summary>
/// Single search result with relevance score and content.
/// Represents a record that matched the search query.
/// </summary>
public sealed class SearchResult
{
    // Identity

    /// <summary>
    /// Record/document ID.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Node ID where this result originated.
    /// Important for multi-node searches.
    /// </summary>
    public required string NodeId { get; init; }

    // Scoring

    /// <summary>
    /// Final relevance score (0.0-1.0) after reranking.
    /// Higher = more relevant.
    /// </summary>
    public required float Relevance { get; init; }

    // FTS-indexed fields (searchable via full-text search)

    /// <summary>
    /// Optional title (FTS-indexed).
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Optional description (FTS-indexed).
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Full content or snippet (FTS-indexed).
    /// If SnippetOnly=true, this contains a snippet.
    /// Otherwise, this contains the full content.
    /// </summary>
    public required string Content { get; init; }

    // Filter-only fields (NOT FTS-indexed, used for exact match/comparison)

    /// <summary>
    /// MIME type of the content.
    /// Filter-only field (NOT FTS-indexed).
    /// </summary>
    public string MimeType { get; init; } = string.Empty;

    /// <summary>
    /// Creation timestamp.
    /// Filter-only field (NOT FTS-indexed).
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Tags for categorization.
    /// Filter-only field (NOT FTS-indexed).
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
    public string[] Tags { get; init; } = [];

    /// <summary>
    /// Custom metadata key-value pairs.
    /// Filter-only field (NOT FTS-indexed).
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}
