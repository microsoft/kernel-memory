// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Search.Models;

namespace KernelMemory.Core.Tests.Search.Models;

/// <summary>
/// Tests for SearchResponse model.
/// </summary>
public sealed class SearchResponseTests
{
    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Arrange
        var results = new SearchResult[]
        {
            new()
            {
                Id = "1",
                NodeId = "personal",
                Relevance = 0.9f,
                Content = "test",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var metadata = new SearchMetadata
        {
            NodesSearched = 1,
            NodesRequested = 1,
            ExecutionTime = TimeSpan.FromMilliseconds(100)
        };

        // Act
        var response = new SearchResponse
        {
            Query = "test query",
            TotalResults = 1,
            Results = results,
            Metadata = metadata
        };

        // Assert
        Assert.Equal("test query", response.Query);
        Assert.Equal(1, response.TotalResults);
        Assert.Same(results, response.Results);
        Assert.Same(metadata, response.Metadata);
    }
}
