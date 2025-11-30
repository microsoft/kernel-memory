// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search.Models;

/// <summary>
/// Configuration for reranking algorithm.
/// Derived from global config + query-time overrides (Q1, Q10).
/// </summary>
public sealed class RerankingConfig
{
    /// <summary>
    /// Per-node weights for relevance scoring.
    /// Key = node ID, Value = weight multiplier.
    /// </summary>
    public required Dictionary<string, float> NodeWeights { get; init; }

    /// <summary>
    /// Per-node, per-index weights for relevance scoring.
    /// Outer key = node ID, Inner key = index ID, Value = weight multiplier.
    /// Example: {"personal": {"fts-main": 0.7, "vector-main": 0.3}}
    /// </summary>
    public required Dictionary<string, Dictionary<string, float>> IndexWeights { get; init; }

    /// <summary>
    /// Diminishing returns multipliers for aggregating multiple appearances of same record.
    /// Default: [1.0, 0.5, 0.25, 0.125] (each multiplier is half of previous).
    /// First appearance: multiplier = 1.0 (full weight)
    /// Second appearance: multiplier = 0.5 (50% boost)
    /// Third appearance: multiplier = 0.25 (25% boost)
    /// Fourth appearance: multiplier = 0.125 (12.5% boost)
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
    public float[] DiminishingMultipliers { get; init; } = SearchConstants.DefaultDiminishingMultipliers;
}
