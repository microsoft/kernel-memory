// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Storage;
using KernelMemory.Core.Storage.Models;
using KernelMemory.Main.Services;
using Moq;
using Xunit;

namespace KernelMemory.Main.Tests.Unit.Services;

/// <summary>
/// Unit tests for ContentService using mocked storage.
/// </summary>
public sealed class ContentServiceTests
{
    [Fact]
    public void Constructor_SetsNodeId()
    {
        // Arrange
        var mockStorage = new Mock<IContentStorage>();
        const string nodeId = "test-node";

        // Act
        var service = new ContentService(mockStorage.Object, nodeId);

        // Assert
        Assert.Equal(nodeId, service.NodeId);
    }

    [Fact]
    public async Task UpsertAsync_CallsStorageUpsert()
    {
        // Arrange
        var mockStorage = new Mock<IContentStorage>();
        const string expectedId = "generated-id-123";
        mockStorage.Setup(s => s.UpsertAsync(It.IsAny<UpsertRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        var service = new ContentService(mockStorage.Object, "test-node");
        var request = new UpsertRequest
        {
            Content = "Test content",
            MimeType = "text/plain"
        };

        // Act
        var result = await service.UpsertAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(expectedId, result);
        mockStorage.Verify(s => s.UpsertAsync(request, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task UpsertAsync_WithCancellationToken_PassesTokenToStorage()
    {
        // Arrange
        var mockStorage = new Mock<IContentStorage>();
        var cts = new CancellationTokenSource();
        const string expectedId = "id-456";
        mockStorage.Setup(s => s.UpsertAsync(It.IsAny<UpsertRequest>(), cts.Token))
            .ReturnsAsync(expectedId);

        var service = new ContentService(mockStorage.Object, "test-node");
        var request = new UpsertRequest { Content = "Content" };

        // Act
        var result = await service.UpsertAsync(request, cts.Token);

        // Assert
        Assert.Equal(expectedId, result);
        mockStorage.Verify(s => s.UpsertAsync(request, cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetAsync_CallsStorageGetById()
    {
        // Arrange
        const string contentId = "test-id";
        var expectedDto = new ContentDto { Id = contentId, Content = "Test content" };
        var mockStorage = new Mock<IContentStorage>();
        mockStorage.Setup(s => s.GetByIdAsync(contentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var service = new ContentService(mockStorage.Object, "test-node");

        // Act
        var result = await service.GetAsync(contentId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(contentId, result.Id);
        Assert.Equal("Test content", result.Content);
        mockStorage.Verify(s => s.GetByIdAsync(contentId, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GetAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var mockStorage = new Mock<IContentStorage>();
        mockStorage.Setup(s => s.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentDto?)null);

        var service = new ContentService(mockStorage.Object, "test-node");

        // Act
        var result = await service.GetAsync("non-existent-id", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_CallsStorageDelete()
    {
        // Arrange
        const string contentId = "delete-id";
        var mockStorage = new Mock<IContentStorage>();
        mockStorage.Setup(s => s.DeleteAsync(contentId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new ContentService(mockStorage.Object, "test-node");

        // Act
        await service.DeleteAsync(contentId, CancellationToken.None);

        // Assert
        mockStorage.Verify(s => s.DeleteAsync(contentId, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ListAsync_CallsStorageList()
    {
        // Arrange
        const int skip = 10;
        const int take = 20;
        var expectedList = new List<ContentDto>
        {
            new() { Id = "id1", Content = "Content 1" },
            new() { Id = "id2", Content = "Content 2" }
        };
        var mockStorage = new Mock<IContentStorage>();
        mockStorage.Setup(s => s.ListAsync(skip, take, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedList);

        var service = new ContentService(mockStorage.Object, "test-node");

        // Act
        var result = await service.ListAsync(skip, take, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("id1", result[0].Id);
        Assert.Equal("id2", result[1].Id);
        mockStorage.Verify(s => s.ListAsync(skip, take, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ListAsync_EmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var mockStorage = new Mock<IContentStorage>();
        mockStorage.Setup(s => s.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ContentDto>());

        var service = new ContentService(mockStorage.Object, "test-node");

        // Act
        var result = await service.ListAsync(0, 10, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task CountAsync_CallsStorageCount()
    {
        // Arrange
        const long expectedCount = 42;
        var mockStorage = new Mock<IContentStorage>();
        mockStorage.Setup(s => s.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        var service = new ContentService(mockStorage.Object, "test-node");

        // Act
        var result = await service.CountAsync(CancellationToken.None);

        // Assert
        Assert.Equal(expectedCount, result);
        mockStorage.Verify(s => s.CountAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CountAsync_EmptyStorage_ReturnsZero()
    {
        // Arrange
        var mockStorage = new Mock<IContentStorage>();
        mockStorage.Setup(s => s.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        var service = new ContentService(mockStorage.Object, "test-node");

        // Act
        var result = await service.CountAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0L, result);
    }
}
