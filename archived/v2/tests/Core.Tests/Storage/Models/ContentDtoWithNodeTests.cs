// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Core.Storage.Models;

namespace KernelMemory.Core.Tests.Storage.Models;

/// <summary>
/// Tests for ContentDtoWithNode model class.
/// </summary>
public sealed class ContentDtoWithNodeTests
{
    [Fact]
    public void ContentDtoWithNode_Properties_CanBeSetAndRetrieved()
    {
        // Arrange & Act
        var dto = new ContentDtoWithNode
        {
            Node = "test-node",
            Id = "content-123",
            Content = "Test content",
            MimeType = "text/plain",
            Title = "Test Title",
            Description = "Test Description",
            ByteSize = 1024,
            Tags = ["tag1", "tag2"],
            Metadata = new Dictionary<string, string>
            {
                ["author"] = "John Doe",
                ["category"] = "documentation"
            },
            ContentCreatedAt = DateTimeOffset.UtcNow.AddDays(-5),
            RecordCreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
            RecordUpdatedAt = DateTimeOffset.UtcNow
        };

        // Assert
        Assert.Equal("test-node", dto.Node);
        Assert.Equal("content-123", dto.Id);
        Assert.Equal("Test content", dto.Content);
        Assert.Equal("text/plain", dto.MimeType);
        Assert.Equal("Test Title", dto.Title);
        Assert.Equal("Test Description", dto.Description);
        Assert.Equal(1024, dto.ByteSize);
        Assert.Equal(2, dto.Tags.Length);
        Assert.Equal("tag1", dto.Tags[0]);
        Assert.Equal("tag2", dto.Tags[1]);
        Assert.Equal(2, dto.Metadata.Count);
        Assert.Equal("John Doe", dto.Metadata["author"]);
        Assert.Equal("documentation", dto.Metadata["category"]);
    }

    [Fact]
    public void ContentDtoWithNode_EmptyCollections_WorksCorrectly()
    {
        // Arrange & Act
        var dto = new ContentDtoWithNode
        {
            Node = "node",
            Id = "id",
            Content = "content",
            MimeType = "text/plain",
            Tags = [],
            Metadata = new Dictionary<string, string>()
        };

        // Assert
        Assert.Empty(dto.Tags);
        Assert.Empty(dto.Metadata);
    }

    [Fact]
    public void ContentDtoWithNode_FromContentDto_MapsAllProperties()
    {
        // Arrange
        var contentDto = new ContentDto
        {
            Id = "test-id",
            Content = "Test content",
            MimeType = "text/plain",
            Title = "Title",
            Description = "Desc",
            ByteSize = 512,
            Tags = ["a", "b"],
            Metadata = new Dictionary<string, string> { ["key"] = "value" },
            ContentCreatedAt = DateTimeOffset.UtcNow,
            RecordCreatedAt = DateTimeOffset.UtcNow,
            RecordUpdatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var result = ContentDtoWithNode.FromContentDto(contentDto, "my-node");

        // Assert
        Assert.Equal("test-id", result.Id);
        Assert.Equal("my-node", result.Node);
        Assert.Equal("Test content", result.Content);
        Assert.Equal("text/plain", result.MimeType);
        Assert.Equal("Title", result.Title);
        Assert.Equal("Desc", result.Description);
        Assert.Equal(512, result.ByteSize);
        Assert.Equal(2, result.Tags.Length);
        Assert.Single(result.Metadata);
    }
}
