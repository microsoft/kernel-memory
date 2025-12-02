// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Search;

namespace KernelMemory.Core.Tests.Search;

/// <summary>
/// Tests for SearchConstants to ensure values are as expected.
/// </summary>
public sealed class SearchConstantsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Verify default values match requirements
        Assert.Equal(0.3f, SearchConstants.DefaultMinRelevance);
        Assert.Equal(20, SearchConstants.DefaultLimit);
        Assert.Equal(30, SearchConstants.DefaultSearchTimeoutSeconds);
        Assert.Equal(1000, SearchConstants.DefaultMaxResultsPerNode);
        Assert.Equal(1.0f, SearchConstants.DefaultNodeWeight);
        Assert.Equal(1.0f, SearchConstants.DefaultIndexWeight);
    }

    [Fact]
    public void QueryComplexityLimits_AreReasonable()
    {
        // Verify query complexity limits are set
        Assert.Equal(10, SearchConstants.MaxQueryDepth);
        Assert.Equal(50, SearchConstants.MaxBooleanOperators);
        Assert.Equal(1000, SearchConstants.MaxFieldValueLength);
        Assert.Equal(1000, SearchConstants.QueryParseTimeoutMs);
    }

    [Fact]
    public void SnippetDefaults_AreConfigured()
    {
        // Verify snippet configuration
        Assert.Equal(200, SearchConstants.DefaultSnippetLength);
        Assert.Equal(1, SearchConstants.DefaultMaxSnippetsPerResult);
        Assert.Equal("...", SearchConstants.DefaultSnippetSeparator);
        Assert.Equal("<mark>", SearchConstants.DefaultHighlightPrefix);
        Assert.Equal("</mark>", SearchConstants.DefaultHighlightSuffix);
    }

    [Fact]
    public void DiminishingMultipliers_FollowPattern()
    {
        // Verify diminishing returns pattern (each is half of previous)
        var multipliers = SearchConstants.DefaultDiminishingMultipliers;
        Assert.Equal(4, multipliers.Length);
        Assert.Equal(1.0f, multipliers[0]);
        Assert.Equal(0.5f, multipliers[1]);
        Assert.Equal(0.25f, multipliers[2]);
        Assert.Equal(0.125f, multipliers[3]);
    }

    [Fact]
    public void RelevanceScoreBounds_AreCorrect()
    {
        // Verify score boundaries
        Assert.Equal(1.0f, SearchConstants.MaxRelevanceScore);
        Assert.Equal(0.0f, SearchConstants.MinRelevanceScore);
    }

    [Fact]
    public void AllNodesWildcard_IsAsterisk()
    {
        // Verify wildcard character
        Assert.Equal("*", SearchConstants.AllNodesWildcard);
    }
}
