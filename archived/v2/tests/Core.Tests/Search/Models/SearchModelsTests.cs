// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Core.Search.Models;

namespace KernelMemory.Core.Tests.Search.Models;

/// <summary>
/// Tests for search model classes that were previously uncovered.
/// </summary>
public sealed class SearchModelsTests
{
    [Fact]
    public void NodeTiming_Properties_CanBeSetAndRetrieved()
    {
        // Arrange & Act
        var timing = new NodeTiming
        {
            NodeId = "test-node",
            SearchTime = TimeSpan.FromMilliseconds(123)
        };

        // Assert
        Assert.Equal("test-node", timing.NodeId);
        Assert.Equal(TimeSpan.FromMilliseconds(123), timing.SearchTime);
    }

    [Fact]
    public void QueryValidationResult_ValidQuery_CreatesSuccessResult()
    {
        // Arrange & Act
        var result = new QueryValidationResult
        {
            IsValid = true,
            ErrorMessage = null,
            ErrorPosition = null,
            AvailableFields = ["content", "title", "description"]
        };

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorPosition);
        Assert.Equal(3, result.AvailableFields.Length);
    }

    [Fact]
    public void QueryValidationResult_InvalidQuery_CreatesErrorResult()
    {
        // Arrange & Act
        var result = new QueryValidationResult
        {
            IsValid = false,
            ErrorMessage = "Syntax error",
            ErrorPosition = 15,
            AvailableFields = []
        };

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Syntax error", result.ErrorMessage);
        Assert.Equal(15, result.ErrorPosition);
        Assert.Empty(result.AvailableFields);
    }
}
