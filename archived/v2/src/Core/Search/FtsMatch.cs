// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search;

/// <summary>
/// Represents a match from a full-text search query.
/// </summary>
public sealed class FtsMatch
{
    /// <summary>
    /// The content ID that matched the search query.
    /// </summary>
    public required string ContentId { get; init; }

    /// <summary>
    /// The relevance score (higher is more relevant).
    /// FTS5 rank is negative (closer to 0 is better), so we negate it.
    /// </summary>
    public required double Score { get; init; }

    /// <summary>
    /// A snippet of the matched text with highlights.
    /// Highlights are marked with configurable markers (default: no markers).
    /// </summary>
    public required string Snippet { get; init; }
}
