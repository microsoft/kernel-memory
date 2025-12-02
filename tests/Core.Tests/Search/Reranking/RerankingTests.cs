// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Search.Models;
using KernelMemory.Core.Search.Reranking;

namespace KernelMemory.Core.Tests.Search.Reranking;

/// <summary>
/// Tests for WeightedDiminishingReranker using explicit examples from requirements doc.
/// Tests score calculation algorithm from requirements section "Score Calculation Reference".
/// </summary>
public sealed class RerankingTests
{
    private readonly WeightedDiminishingReranker _reranker = new();

    [Fact]
    public void Rerank_SingleIndexResult_Example1FromRequirements()
    {
        // Requirements doc lines 1894-1906: Single index result with weights
        // Node: "personal" (node_weight = 1.0)
        // Index: "fts-main" (index_weight = 0.7)
        // base_relevance = 0.8
        // Expected: 0.8 × 0.7 × 1.0 = 0.56

        var config = new RerankingConfig
        {
            NodeWeights = new Dictionary<string, float> { ["personal"] = 1.0f },
            IndexWeights = new Dictionary<string, Dictionary<string, float>>
            {
                ["personal"] = new Dictionary<string, float> { ["fts-main"] = 0.7f }
            },
            DiminishingMultipliers = [1.0f, 0.5f, 0.25f, 0.125f]
        };

        var results = new[]
        {
            new SearchIndexResult
            {
                RecordId = "doc-1",
                NodeId = "personal",
                IndexId = "fts-main",
                BaseRelevance = 0.8f,
                Title = "Test",
                Content = "Content",
                CreatedAt = DateTimeOffset.Now
            }
        };

        var reranked = this._reranker.Rerank(results, config);

        Assert.Single(reranked);
        Assert.Equal(0.56f, reranked[0].Relevance, precision: 2);
    }

    [Fact]
    public void Rerank_DifferentNodeWeight_Example2FromRequirements()
    {
        // Requirements doc lines 1908-1920: Different node weight
        // Node: "archive" (node_weight = 0.5, less important)
        // Index: "fts-main" (index_weight = 0.7)
        // base_relevance = 0.9
        // Expected: 0.9 × 0.7 × 0.5 = 0.315

        var config = new RerankingConfig
        {
            NodeWeights = new Dictionary<string, float> { ["archive"] = 0.5f },
            IndexWeights = new Dictionary<string, Dictionary<string, float>>
            {
                ["archive"] = new Dictionary<string, float> { ["fts-main"] = 0.7f }
            },
            DiminishingMultipliers = [1.0f, 0.5f, 0.25f, 0.125f]
        };

        var results = new[]
        {
            new SearchIndexResult
            {
                RecordId = "doc-1",
                NodeId = "archive",
                IndexId = "fts-main",
                BaseRelevance = 0.9f,
                Title = "Test",
                Content = "Content",
                CreatedAt = DateTimeOffset.Now
            }
        };

        var reranked = this._reranker.Rerank(results, config);

        Assert.Single(reranked);
        Assert.Equal(0.315f, reranked[0].Relevance, precision: 3);
    }

    [Fact]
    public void Rerank_DifferentIndexWeight_Example3FromRequirements()
    {
        // Requirements doc lines 1922-1934: Different index weight
        // Node: "personal" (node_weight = 1.0)
        // Index: "vector-main" (index_weight = 0.3, less reliable than FTS)
        // base_relevance = 0.7
        // Expected: 0.7 × 0.3 × 1.0 = 0.21

        var config = new RerankingConfig
        {
            NodeWeights = new Dictionary<string, float> { ["personal"] = 1.0f },
            IndexWeights = new Dictionary<string, Dictionary<string, float>>
            {
                ["personal"] = new Dictionary<string, float> { ["vector-main"] = 0.3f }
            },
            DiminishingMultipliers = [1.0f, 0.5f, 0.25f, 0.125f]
        };

        var results = new[]
        {
            new SearchIndexResult
            {
                RecordId = "doc-1",
                NodeId = "personal",
                IndexId = "vector-main",
                BaseRelevance = 0.7f,
                Title = "Test",
                Content = "Content",
                CreatedAt = DateTimeOffset.Now
            }
        };

        var reranked = this._reranker.Rerank(results, config);

        Assert.Single(reranked);
        Assert.Equal(0.21f, reranked[0].Relevance, precision: 2);
    }

    [Fact]
    public void Rerank_SameRecordTwoIndexes_Example4FromRequirements()
    {
        // Requirements doc lines 1956-1983: Same record from two indexes with diminishing returns
        // Record "doc-123" appears in FTS and Vector indexes
        // FTS: 0.8 × 0.7 × 1.0 = 0.56
        // Vector: 0.6 × 0.3 × 1.0 = 0.18
        // Aggregation: 0.56×1.0 + 0.18×0.5 = 0.56 + 0.09 = 0.65

        var config = new RerankingConfig
        {
            NodeWeights = new Dictionary<string, float> { ["personal"] = 1.0f },
            IndexWeights = new Dictionary<string, Dictionary<string, float>>
            {
                ["personal"] = new Dictionary<string, float>
                {
                    ["fts-main"] = 0.7f,
                    ["vector-main"] = 0.3f
                }
            },
            DiminishingMultipliers = [1.0f, 0.5f, 0.25f, 0.125f]
        };

        var results = new[]
        {
            new SearchIndexResult
            {
                RecordId = "doc-123",
                NodeId = "personal",
                IndexId = "fts-main",
                BaseRelevance = 0.8f,
                Title = "Test",
                Content = "Content",
                CreatedAt = DateTimeOffset.Now
            },
            new SearchIndexResult
            {
                RecordId = "doc-123",
                NodeId = "personal",
                IndexId = "vector-main",
                BaseRelevance = 0.6f,
                Title = "Test",
                Content = "Content",
                CreatedAt = DateTimeOffset.Now
            }
        };

        var reranked = this._reranker.Rerank(results, config);

        Assert.Single(reranked); // Same record, so only one result after merging
        Assert.Equal("doc-123", reranked[0].Id);
        Assert.Equal(0.65f, reranked[0].Relevance, precision: 2);
    }

    [Fact]
    public void Rerank_SameRecordThreeIndexes_Example5FromRequirements()
    {
        // Requirements doc lines 1985-2000: Same record from three indexes
        // Record "doc-456" appears in FTS, Vector, and another index
        // FTS: 0.9 × 0.7 × 1.0 = 0.63
        // Vector: 0.8 × 0.3 × 1.0 = 0.24
        // Third: 0.5 × 0.5 × 1.0 = 0.25
        // Aggregation: 0.63×1.0 + 0.25×0.5 + 0.24×0.25 = 0.63 + 0.125 + 0.06 = 0.815

        var config = new RerankingConfig
        {
            NodeWeights = new Dictionary<string, float> { ["personal"] = 1.0f },
            IndexWeights = new Dictionary<string, Dictionary<string, float>>
            {
                ["personal"] = new Dictionary<string, float>
                {
                    ["fts-main"] = 0.7f,
                    ["vector-main"] = 0.3f,
                    ["fts-secondary"] = 0.5f
                }
            },
            DiminishingMultipliers = [1.0f, 0.5f, 0.25f, 0.125f]
        };

        var results = new[]
        {
            new SearchIndexResult
            {
                RecordId = "doc-456",
                NodeId = "personal",
                IndexId = "fts-main",
                BaseRelevance = 0.9f,
                Title = "Test",
                Content = "Content",
                CreatedAt = DateTimeOffset.Now
            },
            new SearchIndexResult
            {
                RecordId = "doc-456",
                NodeId = "personal",
                IndexId = "fts-secondary",
                BaseRelevance = 0.5f,
                Title = "Test",
                Content = "Content",
                CreatedAt = DateTimeOffset.Now
            },
            new SearchIndexResult
            {
                RecordId = "doc-456",
                NodeId = "personal",
                IndexId = "vector-main",
                BaseRelevance = 0.8f,
                Title = "Test",
                Content = "Content",
                CreatedAt = DateTimeOffset.Now
            }
        };

        var reranked = this._reranker.Rerank(results, config);

        Assert.Single(reranked);
        Assert.Equal("doc-456", reranked[0].Id);
        // Sorted by weighted score: 0.63, 0.25, 0.24
        // 0.63×1.0 + 0.25×0.5 + 0.24×0.25 = 0.815
        Assert.Equal(0.815f, reranked[0].Relevance, precision: 3);
    }

    [Fact]
    public void Rerank_MultipleRecords_SortsCorrectly()
    {
        var config = new RerankingConfig
        {
            NodeWeights = new Dictionary<string, float> { ["personal"] = 1.0f },
            IndexWeights = new Dictionary<string, Dictionary<string, float>>
            {
                ["personal"] = new Dictionary<string, float> { ["fts-main"] = 1.0f }
            },
            DiminishingMultipliers = [1.0f, 0.5f, 0.25f, 0.125f]
        };

        var now = DateTimeOffset.Now;
        var results = new[]
        {
            new SearchIndexResult
            {
                RecordId = "doc-1",
                NodeId = "personal",
                IndexId = "fts-main",
                BaseRelevance = 0.7f,
                Title = "Test",
                Content = "Content",
                CreatedAt = now.AddDays(-2)
            },
            new SearchIndexResult
            {
                RecordId = "doc-2",
                NodeId = "personal",
                IndexId = "fts-main",
                BaseRelevance = 0.9f,
                Title = "Test",
                Content = "Content",
                CreatedAt = now.AddDays(-1)
            },
            new SearchIndexResult
            {
                RecordId = "doc-3",
                NodeId = "personal",
                IndexId = "fts-main",
                BaseRelevance = 0.9f, // Same relevance as doc-2
                Title = "Test",
                Content = "Content",
                CreatedAt = now // Newer than doc-2
            }
        };

        var reranked = this._reranker.Rerank(results, config);

        Assert.Equal(3, reranked.Length);

        // Should be sorted by relevance DESC, then by createdAt DESC (recency bias)
        Assert.Equal("doc-3", reranked[0].Id); // 0.9 relevance, newest
        Assert.Equal("doc-2", reranked[1].Id); // 0.9 relevance, older
        Assert.Equal("doc-1", reranked[2].Id); // 0.7 relevance
    }

    [Fact]
    public void Rerank_ScoreCappedAtOne_WhenExceedsMaximum()
    {
        // If weighted scores sum to more than 1.0, cap at 1.0
        var config = new RerankingConfig
        {
            NodeWeights = new Dictionary<string, float> { ["personal"] = 1.0f },
            IndexWeights = new Dictionary<string, Dictionary<string, float>>
            {
                ["personal"] = new Dictionary<string, float>
                {
                    ["fts-main"] = 1.0f,
                    ["vector-main"] = 1.0f
                }
            },
            DiminishingMultipliers = [1.0f, 0.5f, 0.25f, 0.125f]
        };

        var results = new[]
        {
            new SearchIndexResult
            {
                RecordId = "doc-1",
                NodeId = "personal",
                IndexId = "fts-main",
                BaseRelevance = 1.0f, // Perfect match
                Title = "Test",
                Content = "Content",
                CreatedAt = DateTimeOffset.Now
            },
            new SearchIndexResult
            {
                RecordId = "doc-1",
                NodeId = "personal",
                IndexId = "vector-main",
                BaseRelevance = 0.9f, // Also very high
                Title = "Test",
                Content = "Content",
                CreatedAt = DateTimeOffset.Now
            }
        };

        var reranked = this._reranker.Rerank(results, config);

        Assert.Single(reranked);
        // Weighted: 1.0×1.0 + 0.9×0.5 = 1.0 + 0.45 = 1.45
        // But capped at 1.0
        Assert.Equal(1.0f, reranked[0].Relevance);
    }

    [Fact]
    public void Rerank_EmptyResults_ReturnsEmptyArray()
    {
        var config = new RerankingConfig
        {
            NodeWeights = new Dictionary<string, float>(),
            IndexWeights = new Dictionary<string, Dictionary<string, float>>(),
            DiminishingMultipliers = [1.0f, 0.5f, 0.25f, 0.125f]
        };

        var results = Array.Empty<SearchIndexResult>();

        var reranked = this._reranker.Rerank(results, config);

        Assert.Empty(reranked);
    }

    [Fact]
    public void Rerank_UsesHighestScoredAppearanceForRecordData()
    {
        // When same record appears multiple times, use the highest-scored appearance for the data
        var config = new RerankingConfig
        {
            NodeWeights = new Dictionary<string, float> { ["personal"] = 1.0f },
            IndexWeights = new Dictionary<string, Dictionary<string, float>>
            {
                ["personal"] = new Dictionary<string, float>
                {
                    ["fts-main"] = 0.7f,
                    ["vector-main"] = 0.3f
                }
            },
            DiminishingMultipliers = [1.0f, 0.5f, 0.25f, 0.125f]
        };

        var results = new[]
        {
            new SearchIndexResult
            {
                RecordId = "doc-1",
                NodeId = "personal",
                IndexId = "fts-main",
                BaseRelevance = 0.9f,
                Title = "FTS Title", // This should be used (highest weighted score)
                Content = "FTS Content",
                CreatedAt = DateTimeOffset.Now
            },
            new SearchIndexResult
            {
                RecordId = "doc-1",
                NodeId = "personal",
                IndexId = "vector-main",
                BaseRelevance = 0.8f,
                Title = "Vector Title",
                Content = "Vector Content",
                CreatedAt = DateTimeOffset.Now
            }
        };

        var reranked = this._reranker.Rerank(results, config);

        Assert.Single(reranked);
        // Should use data from FTS result (higher weighted score: 0.9×0.7=0.63 vs 0.8×0.3=0.24)
        Assert.Equal("FTS Title", reranked[0].Title);
        Assert.Equal("FTS Content", reranked[0].Content);
    }
}
