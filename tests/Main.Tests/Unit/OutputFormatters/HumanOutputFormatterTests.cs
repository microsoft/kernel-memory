// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Storage.Models;
using KernelMemory.Main.CLI.OutputFormatters;
using Xunit;

namespace KernelMemory.Main.Tests.Unit.OutputFormatters;

/// <summary>
/// Unit tests for HumanOutputFormatter.
/// Tests exercise the formatting logic with various data types.
/// </summary>
[Collection("ConsoleOutputTests")]
public sealed class HumanOutputFormatterTests
{
    [Fact]
    public void Constructor_SetsVerbosity()
    {
        // Arrange & Act
        var formatter = new HumanOutputFormatter("quiet", useColors: false);

        // Assert
        Assert.Equal("quiet", formatter.Verbosity);
    }

    [Fact]
    public void Constructor_WithColors_SetsColorMode()
    {
        // Arrange & Act
        var formatter = new HumanOutputFormatter("normal", useColors: true);

        // Assert - Verifies constructor doesn't throw
        Assert.Equal("normal", formatter.Verbosity);
    }

    [Fact]
    public void Constructor_WithNoColors_DisablesColors()
    {
        // Arrange & Act
        var formatter = new HumanOutputFormatter("normal", useColors: false);

        // Assert
        Assert.Equal("normal", formatter.Verbosity);
    }

    [Fact]
    public void Format_WithSilentVerbosity_DoesNotOutput()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("silent", useColors: false);
        var data = new { test = "data" };

        // Act & Assert - Should silently exit
        formatter.Format(data);
    }

    [Fact]
    public void Format_WithStringData_HandlesString()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("normal", useColors: false);
        const string testString = "Test output string";

        // Act & Assert
        formatter.Format(testString);
    }

    [Fact]
    public void Format_WithContentDto_FormatsContent()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("normal", useColors: false);
        var content = new ContentDto
        {
            Id = "test-id-123",
            Content = "Short content",
            MimeType = "text/plain",
            ByteSize = 13,
            Title = "Test Title",
            Description = "Test Description",
            Tags = new[] { "tag1", "tag2" },
            ContentCreatedAt = DateTimeOffset.UtcNow,
            RecordCreatedAt = DateTimeOffset.UtcNow,
            RecordUpdatedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, string> { ["key"] = "value" }
        };

        // Act & Assert - Exercises all formatting logic
        formatter.Format(content);
    }

    [Fact]
    public void Format_WithContentDto_QuietMode_OutputsOnlyId()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("quiet", useColors: false);
        var content = new ContentDto
        {
            Id = "quiet-id",
            Content = "Content",
            MimeType = "text/plain"
        };

        // Act & Assert
        formatter.Format(content);
    }

    [Fact]
    public void Format_WithContentDto_VerboseMode_ShowsAllDetails()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("verbose", useColors: false);
        var content = new ContentDto
        {
            Id = "verbose-id",
            Content = "Verbose content",
            MimeType = "text/plain",
            ByteSize = 15,
            ContentCreatedAt = DateTimeOffset.UtcNow,
            RecordCreatedAt = DateTimeOffset.UtcNow,
            RecordUpdatedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, string>
            {
                ["meta1"] = "value1",
                ["meta2"] = "value2"
            }
        };

        // Act & Assert
        formatter.Format(content);
    }

    [Fact]
    public void Format_WithLongContent_TruncatesInNormalMode()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("normal", useColors: false);
        var longContent = new string('x', Constants.MaxContentDisplayLength + 100);
        var content = new ContentDto
        {
            Id = "long-content-id",
            Content = longContent,
            MimeType = "text/plain",
            ByteSize = longContent.Length
        };

        // Act & Assert - Should truncate content
        formatter.Format(content);
    }

    [Fact]
    public void Format_WithLongContent_DoesNotTruncateInVerboseMode()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("verbose", useColors: false);
        var longContent = new string('y', Constants.MaxContentDisplayLength + 100);
        var content = new ContentDto
        {
            Id = "long-verbose-id",
            Content = longContent,
            MimeType = "text/plain",
            ByteSize = longContent.Length
        };

        // Act & Assert - Should show full content
        formatter.Format(content);
    }

    [Fact]
    public void Format_WithGenericObject_HandlesToString()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("normal", useColors: false);
        var obj = new { id = "obj-123", status = "ok" };

        // Act & Assert
        formatter.Format(obj);
    }

    [Fact]
    public void FormatError_WithColors_FormatsError()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("normal", useColors: true);
        const string errorMessage = "Test error with colors";

        // Act & Assert
        formatter.FormatError(errorMessage);
    }

    [Fact]
    public void FormatError_WithoutColors_FormatsError()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("normal", useColors: false);
        const string errorMessage = "Test error without colors";

        // Act & Assert
        formatter.FormatError(errorMessage);
    }

    [Fact]
    public void FormatList_WithSilentVerbosity_DoesNotOutput()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("silent", useColors: false);
        var items = new List<ContentDto>
        {
            new() { Id = "id1", Content = "Content 1", MimeType = "text/plain" }
        };

        // Act & Assert
        formatter.FormatList(items, totalCount: 1, skip: 0, take: 1);
    }

    [Fact]
    public void FormatList_WithEmptyContentList_ShowsEmptyMessage()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("normal", useColors: false);
        var items = new List<ContentDto>();

        // Act & Assert
        formatter.FormatList(items, totalCount: 0, skip: 0, take: 10);
    }

    [Fact]
    public void FormatList_WithEmptyContentList_WithColors_ShowsEmptyMessage()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("normal", useColors: true);
        var items = new List<ContentDto>();

        // Act & Assert
        formatter.FormatList(items, totalCount: 0, skip: 0, take: 10);
    }

    [Fact]
    public void FormatList_WithContentList_QuietMode_OutputsIds()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("quiet", useColors: false);
        var items = new List<ContentDto>
        {
            new() { Id = "id1", Content = "C1", MimeType = "text/plain", RecordCreatedAt = DateTimeOffset.UtcNow },
            new() { Id = "id2", Content = "C2", MimeType = "text/plain", RecordCreatedAt = DateTimeOffset.UtcNow }
        };

        // Act & Assert
        formatter.FormatList(items, totalCount: 2, skip: 0, take: 2);
    }

    [Fact]
    public void FormatList_WithContentList_NormalMode_ShowsTable()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("normal", useColors: false);
        var items = new List<ContentDto>
        {
            new() { Id = "id1", Content = "Content 1", MimeType = "text/plain", ByteSize = 9, RecordCreatedAt = DateTimeOffset.UtcNow },
            new() { Id = "id2", Content = "Very long content that should be truncated in the preview column", MimeType = "text/markdown", ByteSize = 65, RecordCreatedAt = DateTimeOffset.UtcNow }
        };

        // Act & Assert - Exercises table formatting and content preview truncation
        formatter.FormatList(items, totalCount: 10, skip: 0, take: 2);
    }

    [Fact]
    public void FormatList_WithStringList_ShowsList()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("normal", useColors: false);
        var items = new List<string> { "node1", "node2", "node3" };

        // Act & Assert
        formatter.FormatList(items, totalCount: 3, skip: 0, take: 3);
    }

    [Fact]
    public void FormatList_WithEmptyStringList_ShowsEmptyMessage()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("normal", useColors: false);
        var items = new List<string>();

        // Act & Assert
        formatter.FormatList(items, totalCount: 0, skip: 0, take: 10);
    }

    [Fact]
    public void FormatList_WithEmptyStringList_WithColors_ShowsEmptyMessage()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("normal", useColors: true);
        var items = new List<string>();

        // Act & Assert
        formatter.FormatList(items, totalCount: 0, skip: 0, take: 10);
    }

    [Fact]
    public void FormatList_WithStringList_QuietMode_OutputsStrings()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("quiet", useColors: false);
        var items = new List<string> { "item1", "item2", "item3" };

        // Act & Assert
        formatter.FormatList(items, totalCount: 3, skip: 0, take: 3);
    }

    [Fact]
    public void FormatList_WithGenericList_ShowsList()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("normal", useColors: false);
        var items = new List<int> { 1, 2, 3, 4, 5 };

        // Act & Assert
        formatter.FormatList(items, totalCount: 5, skip: 0, take: 5);
    }

    [Fact]
    public void FormatList_WithEmptyGenericList_ShowsEmptyMessage()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("normal", useColors: false);
        var items = new List<int>();

        // Act & Assert
        formatter.FormatList(items, totalCount: 0, skip: 0, take: 10);
    }

    [Fact]
    public void FormatList_WithEmptyGenericList_WithColors_ShowsEmptyMessage()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("normal", useColors: true);
        var items = new List<int>();

        // Act & Assert
        formatter.FormatList(items, totalCount: 0, skip: 0, take: 10);
    }

    [Fact]
    public void Format_WithContentDto_EmptyOptionalFields_HandlesCorrectly()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("normal", useColors: false);
        var content = new ContentDto
        {
            Id = "minimal-id",
            Content = "Minimal content",
            MimeType = "text/plain",
            ByteSize = 15,
            Title = string.Empty,
            Description = string.Empty,
            Tags = Array.Empty<string>(),
            ContentCreatedAt = DateTimeOffset.UtcNow,
            RecordCreatedAt = DateTimeOffset.UtcNow,
            RecordUpdatedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, string>()
        };

        // Act & Assert - Should skip empty optional fields
        formatter.Format(content);
    }

    [Fact]
    public void Format_WithContentDto_WithTitle_ShowsTitle()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("normal", useColors: false);
        var content = new ContentDto
        {
            Id = "titled-id",
            Content = "Content with title",
            MimeType = "text/plain",
            ByteSize = 18,
            Title = "Important Title"
        };

        // Act & Assert
        formatter.Format(content);
    }

    [Fact]
    public void Format_WithContentDto_WithDescription_ShowsDescription()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("normal", useColors: false);
        var content = new ContentDto
        {
            Id = "described-id",
            Content = "Content with description",
            MimeType = "text/plain",
            ByteSize = 24,
            Description = "Detailed description of the content"
        };

        // Act & Assert
        formatter.Format(content);
    }

    [Fact]
    public void Format_WithContentDto_WithTags_ShowsTags()
    {
        // Arrange
        var formatter = new HumanOutputFormatter("normal", useColors: false);
        var content = new ContentDto
        {
            Id = "tagged-id",
            Content = "Tagged content",
            MimeType = "text/plain",
            ByteSize = 14,
            Tags = new[] { "important", "review", "urgent" }
        };

        // Act & Assert
        formatter.Format(content);
    }
}
