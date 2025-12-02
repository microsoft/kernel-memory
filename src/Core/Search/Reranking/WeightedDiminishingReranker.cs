// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Search.Models;

namespace KernelMemory.Core.Search.Reranking;

/// <summary>
/// Default reranking implementation using weighted diminishing returns algorithm.
///
/// Score Calculation:
/// 1. Each index result: weighted_score = base_relevance × index_weight × node_weight
/// 2. Same record from multiple sources: apply diminishing returns
///    - 1st score: multiplier = 1.0 (full weight)
///    - 2nd score: multiplier = 0.5 (50% boost)
///    - 3rd score: multiplier = 0.25 (25% boost)
///    - 4th score: multiplier = 0.125 (12.5% boost)
///    - Formula: final = score1×1.0 + score2×0.5 + score3×0.25 + ...
/// 3. Final score capped at 1.0
///
/// See requirements doc "Score Calculation Reference" section for explicit examples.
/// </summary>
public sealed class WeightedDiminishingReranker : ISearchReranker
{
    /// <summary>
    /// Rerank search results using weighted diminishing returns.
    /// </summary>
    public SearchResult[] Rerank(SearchIndexResult[] results, RerankingConfig config)
    {
        if (results.Length == 0)
        {
            return [];
        }

        // Phase 1: Apply weights to each index result
        var weightedResults = results.Select(r => (
            Result: r,
            WeightedScore: this.ApplyWeights(r, config)
        )).ToList();

        // Phase 2: Group by record ID and aggregate with diminishing returns
        var aggregated = weightedResults
            .GroupBy(r => r.Result.RecordId)
            .Select(group => this.AggregateRecord(group.Key, [.. group], config))
            .ToArray();

        // Sort by final relevance (descending), then by createdAt (descending) for recency bias
        return aggregated
            .OrderByDescending(r => r.Relevance)
            .ThenByDescending(r => r.CreatedAt)
            .ToArray();
    }

    /// <summary>
    /// Apply node and index weights to a single index result.
    /// Formula: weighted_score = base_relevance × index_weight × node_weight
    /// </summary>
    private float ApplyWeights(SearchIndexResult result, RerankingConfig config)
    {
        // Get node weight (default to 1.0 if not configured)
        var nodeWeight = config.NodeWeights.TryGetValue(result.NodeId, out var nw)
            ? nw
            : SearchConstants.DefaultNodeWeight;

        // Get index weight (default to 1.0 if not configured)
        var indexWeight = SearchConstants.DefaultIndexWeight;
        if (config.IndexWeights.TryGetValue(result.NodeId, out var nodeIndexes))
        {
            if (nodeIndexes.TryGetValue(result.IndexId, out var iw))
            {
                indexWeight = iw;
            }
        }

        // Apply weights: base_relevance × index_weight × node_weight
        var weighted = result.BaseRelevance * indexWeight * nodeWeight;

        return weighted;
    }

    /// <summary>
    /// Aggregate multiple appearances of the same record with diminishing returns.
    /// When same record appears in multiple indexes/chunks, boost the score but with diminishing returns.
    /// </summary>
    private SearchResult AggregateRecord(
        string recordId,
        (SearchIndexResult Result, float WeightedScore)[] appearances,
        RerankingConfig config)
    {
        // Sort appearances by weighted score (descending)
        var sorted = appearances.OrderByDescending(a => a.WeightedScore).ToArray();

        // Apply diminishing returns multipliers
        float finalScore = 0f;
        var multipliers = config.DiminishingMultipliers;

        for (int i = 0; i < sorted.Length; i++)
        {
            var score = sorted[i].WeightedScore;
            var multiplier = i < multipliers.Length
                ? multipliers[i]
                : multipliers[^1] * (float)Math.Pow(0.5, i - multipliers.Length + 1); // Continue halving

            finalScore += score * multiplier;
        }

        // Cap at 1.0 (max relevance)
        if (finalScore > SearchConstants.MaxRelevanceScore)
        {
            finalScore = SearchConstants.MaxRelevanceScore;
        }

        // Use the highest-scored appearance for the record data
        var bestAppearance = sorted[0].Result;

        // Build the final search result
        return new SearchResult
        {
            Id = recordId,
            NodeId = bestAppearance.NodeId,
            Relevance = finalScore,
            Title = bestAppearance.Title,
            Description = bestAppearance.Description,
            Content = bestAppearance.Content,
            MimeType = bestAppearance.MimeType,
            CreatedAt = bestAppearance.CreatedAt,
            Tags = bestAppearance.Tags,
            Metadata = bestAppearance.Metadata
        };
    }
}
