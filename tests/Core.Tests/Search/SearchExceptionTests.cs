// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Search.Exceptions;

namespace KernelMemory.Core.Tests.Search;

/// <summary>
/// Tests for SearchException to ensure proper error handling.
/// </summary>
public sealed class SearchExceptionTests
{
    [Fact]
    public void Constructor_WithErrorType_SetsProperties()
    {
        // Arrange
        const string message = "Node not found";
        const string nodeId = "test-node";
        const SearchErrorType errorType = SearchErrorType.NodeNotFound;

        // Act
        var exception = new SearchException(message, errorType, nodeId);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(errorType, exception.ErrorType);
        Assert.Equal(nodeId, exception.NodeId);
    }

    [Fact]
    public void Constructor_WithInnerException_SetsProperties()
    {
        // Arrange
        const string message = "Query failed";
        var innerException = new InvalidOperationException("Inner error");
        const SearchErrorType errorType = SearchErrorType.QuerySyntaxError;

        // Act
        var exception = new SearchException(message, errorType, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(errorType, exception.ErrorType);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void Constructor_WithoutNodeId_NodeIdIsNull()
    {
        // Arrange & Act
        var exception = new SearchException("Error", SearchErrorType.QueryTooComplex);

        // Assert
        Assert.Null(exception.NodeId);
    }

    [Fact]
    public void StandardConstructors_Work()
    {
        // Test standard exception constructors
        var ex1 = new SearchException();
        Assert.NotNull(ex1);

        var ex2 = new SearchException("Test message");
        Assert.Equal("Test message", ex2.Message);

        var inner = new InvalidOperationException();
        var ex3 = new SearchException("Test message", inner);
        Assert.Equal("Test message", ex3.Message);
        Assert.Same(inner, ex3.InnerException);
    }

    [Theory]
    [InlineData(SearchErrorType.NodeNotFound)]
    [InlineData(SearchErrorType.NodeAccessDenied)]
    [InlineData(SearchErrorType.NodeTimeout)]
    [InlineData(SearchErrorType.IndexNotFound)]
    [InlineData(SearchErrorType.QuerySyntaxError)]
    [InlineData(SearchErrorType.InvalidConfiguration)]
    public void ErrorType_AllTypesCanBeSet(SearchErrorType errorType)
    {
        // Arrange & Act
        var exception = new SearchException("Test", errorType);

        // Assert
        Assert.Equal(errorType, exception.ErrorType);
    }
}
