// Copyright (c) Microsoft. All rights reserved.
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
        Assert.Equal(0.3f, Constants.SearchDefaults.DefaultMinRelevance);
        Assert.Equal(20, Constants.SearchDefaults.DefaultLimit);
        Assert.Equal(30, Constants.SearchDefaults.DefaultSearchTimeoutSeconds);
        Assert.Equal(1000, Constants.SearchDefaults.DefaultMaxResultsPerNode);
        Assert.Equal(1.0f, Constants.SearchDefaults.DefaultNodeWeight);
        Assert.Equal(1.0f, Constants.SearchDefaults.DefaultIndexWeight);
    }

    [Fact]
    public void QueryComplexityLimits_AreReasonable()
    {
        // Verify query complexity limits are set
        Assert.Equal(10, Constants.SearchDefaults.MaxQueryDepth);
        Assert.Equal(50, Constants.SearchDefaults.MaxBooleanOperators);
        Assert.Equal(1000, Constants.SearchDefaults.MaxFieldValueLength);
        Assert.Equal(1000, Constants.SearchDefaults.QueryParseTimeoutMs);
    }

    [Fact]
    public void SnippetDefaults_AreConfigured()
    {
        // Verify snippet configuration
        Assert.Equal(200, Constants.SearchDefaults.DefaultSnippetLength);
        Assert.Equal(1, Constants.SearchDefaults.DefaultMaxSnippetsPerResult);
        Assert.Equal("...", Constants.SearchDefaults.DefaultSnippetSeparator);
        Assert.Equal("<mark>", Constants.SearchDefaults.DefaultHighlightPrefix);
        Assert.Equal("</mark>", Constants.SearchDefaults.DefaultHighlightSuffix);
    }

    [Fact]
    public void DiminishingMultipliers_FollowPattern()
    {
        // Verify diminishing returns pattern (each is half of previous)
        var multipliers = Constants.SearchDefaults.DefaultDiminishingMultipliers;
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
        Assert.Equal(1.0f, Constants.SearchDefaults.MaxRelevanceScore);
        Assert.Equal(0.0f, Constants.SearchDefaults.MinRelevanceScore);
    }

    [Fact]
    public void AllNodesWildcard_IsAsterisk()
    {
        // Verify wildcard character
        Assert.Equal("*", Constants.SearchDefaults.AllNodesWildcard);
    }
}
