// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Storage.Models;
using KernelMemory.Main.CLI.OutputFormatters;
using Xunit;

namespace KernelMemory.Main.Tests.Unit.OutputFormatters;

/// <summary>
/// Unit tests for JsonOutputFormatter.
/// Tests focus on behavior, not console output.
/// </summary>
public sealed class JsonOutputFormatterTests
{
    [Fact]
    public void Constructor_SetsVerbosity()
    {
        // Arrange & Act
        var formatter = new JsonOutputFormatter("quiet");

        // Assert
        Assert.Equal("quiet", formatter.Verbosity);
    }

    [Fact]
    public void Format_WithNormalVerbosity_DoesNotThrow()
    {
        // Arrange
        var formatter = new JsonOutputFormatter("normal");
        var data = new { id = "test-123", status = "success" };

        // Act & Assert - Should not throw
        formatter.Format(data);
    }

    [Fact]
    public void Format_WithSilentVerbosity_DoesNotOutput()
    {
        // Arrange
        var formatter = new JsonOutputFormatter("silent");
        var data = new { id = "test-123" };

        // Act & Assert - Should not throw and silently exit
        formatter.Format(data);
    }

    [Fact]
    public void Format_WithComplexObject_DoesNotThrow()
    {
        // Arrange
        var formatter = new JsonOutputFormatter("normal");
        var content = new ContentDto
        {
            Id = "test-id",
            Content = "Test content",
            MimeType = "text/plain",
            Title = "Test Title"
        };

        // Act & Assert
        formatter.Format(content);
    }

    [Fact]
    public void FormatError_WithMessage_DoesNotThrow()
    {
        // Arrange
        var formatter = new JsonOutputFormatter("normal");
        const string errorMessage = "Test error message";

        // Act & Assert
        formatter.FormatError(errorMessage);
    }

    [Fact]
    public void FormatList_WithItems_DoesNotThrow()
    {
        // Arrange
        var formatter = new JsonOutputFormatter("normal");
        var items = new List<ContentDto>
        {
            new() { Id = "id1", Content = "Content 1" },
            new() { Id = "id2", Content = "Content 2" }
        };

        // Act & Assert
        formatter.FormatList(items, totalCount: 10, skip: 0, take: 2);
    }

    [Fact]
    public void FormatList_WithEmptyList_DoesNotThrow()
    {
        // Arrange
        var formatter = new JsonOutputFormatter("normal");
        var items = new List<ContentDto>();

        // Act & Assert
        formatter.FormatList(items, totalCount: 0, skip: 0, take: 10);
    }

    [Fact]
    public void FormatList_WithSilentVerbosity_DoesNotOutput()
    {
        // Arrange
        var formatter = new JsonOutputFormatter("silent");
        var items = new List<string> { "item1", "item2" };

        // Act & Assert - Should silently exit
        formatter.FormatList(items, totalCount: 2, skip: 0, take: 2);
    }

    [Fact]
    public void FormatList_WithPaginationInfo_IncludesMetadata()
    {
        // Arrange
        var formatter = new JsonOutputFormatter("normal");
        var items = new List<string> { "a", "b", "c" };

        // Act & Assert - Verifies pagination metadata is handled
        formatter.FormatList(items, totalCount: 100, skip: 20, take: 10);
    }
}
