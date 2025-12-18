// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search;

/// <summary>
/// Represents a match from a vector similarity search.
/// Score is a dot product of normalized vectors (range 0-1, where 1 is most similar).
/// </summary>
public sealed class VectorMatch
{
    /// <summary>
    /// The content ID that matched the search query.
    /// </summary>
    public required string ContentId { get; init; }

    /// <summary>
    /// The similarity score (dot product of normalized vectors).
    /// Range: 0-1, where 1 indicates highest similarity.
    /// </summary>
    public required double Score { get; init; }
}
