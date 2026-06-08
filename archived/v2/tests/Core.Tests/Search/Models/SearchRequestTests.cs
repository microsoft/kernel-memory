// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Search.Models;

namespace KernelMemory.Core.Tests.Search.Models;

/// <summary>
/// Tests for SearchRequest model initialization and defaults.
/// </summary>
public sealed class SearchRequestTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var request = new SearchRequest { Query = "test" };

        // Assert - verify defaults
        Assert.Equal("test", request.Query);
        Assert.Empty(request.Nodes);
        Assert.Empty(request.ExcludeNodes);
        Assert.Empty(request.SearchIndexes);
        Assert.Empty(request.ExcludeIndexes);
        Assert.Equal(Constants.SearchDefaults.DefaultLimit, request.Limit);
        Assert.Equal(0, request.Offset);
        Assert.Equal(Constants.SearchDefaults.DefaultMinRelevance, request.MinRelevance);
        Assert.Null(request.MaxResultsPerNode);
        Assert.Null(request.NodeWeights);
        Assert.False(request.SnippetOnly);
        Assert.Null(request.SnippetLength);
        Assert.Null(request.MaxSnippetsPerResult);
        Assert.False(request.Highlight);
        Assert.False(request.WaitForIndexing);
        Assert.Null(request.TimeoutSeconds);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        // Arrange & Act
        var request = new SearchRequest
        {
            Query = "kubernetes",
            Nodes = ["personal", "work"],
            ExcludeNodes = ["archive"],
            Limit = 50,
            Offset = 10,
            MinRelevance = 0.5f,
            MaxResultsPerNode = 500,
            NodeWeights = new Dictionary<string, float> { ["personal"] = 1.0f },
            SnippetOnly = true,
            Highlight = true,
            WaitForIndexing = true,
            TimeoutSeconds = 60
        };

        // Assert
        Assert.Equal("kubernetes", request.Query);
        Assert.Equal(2, request.Nodes.Length);
        Assert.Single(request.ExcludeNodes);
        Assert.Equal(50, request.Limit);
        Assert.Equal(10, request.Offset);
        Assert.Equal(0.5f, request.MinRelevance);
        Assert.Equal(500, request.MaxResultsPerNode);
        Assert.NotNull(request.NodeWeights);
        Assert.True(request.SnippetOnly);
        Assert.True(request.Highlight);
        Assert.True(request.WaitForIndexing);
        Assert.Equal(60, request.TimeoutSeconds);
    }
}
