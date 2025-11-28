// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Main.CLI.Models;
using Xunit;

namespace KernelMemory.Main.Tests.Unit.Models;

/// <summary>
/// Unit tests for DTO models to ensure proper initialization.
/// </summary>
public sealed class DtoTests
{
    [Fact]
    public void CacheConfigDto_InitializesCorrectly()
    {
        // Arrange & Act
        var dto = new CacheConfigDto
        {
            Type = "DiskCache",
            Path = "/path/to/cache"
        };

        // Assert
        Assert.Equal("DiskCache", dto.Type);
        Assert.Equal("/path/to/cache", dto.Path);
    }

    [Fact]
    public void CacheConfigDto_WithNullValues_HandlesCorrectly()
    {
        // Arrange & Act
        var dto = new CacheConfigDto
        {
            Type = null!,
            Path = null!
        };

        // Assert
        Assert.Null(dto.Type);
        Assert.Null(dto.Path);
    }

    [Fact]
    public void CacheInfoDto_InitializesCorrectly()
    {
        // Arrange & Act
        var dto = new CacheInfoDto
        {
            EmbeddingsCache = new CacheConfigDto { Type = "Type1", Path = "/path1" },
            LlmCache = new CacheConfigDto { Type = "Type2", Path = "/path2" }
        };

        // Assert
        Assert.NotNull(dto.EmbeddingsCache);
        Assert.NotNull(dto.LlmCache);
        Assert.Equal("Type1", dto.EmbeddingsCache.Type);
        Assert.Equal("Type2", dto.LlmCache.Type);
    }

    [Fact]
    public void CacheInfoDto_WithNullCaches_HandlesCorrectly()
    {
        // Arrange & Act
        var dto = new CacheInfoDto
        {
            EmbeddingsCache = null,
            LlmCache = null
        };

        // Assert
        Assert.Null(dto.EmbeddingsCache);
        Assert.Null(dto.LlmCache);
    }

    [Fact]
    public void ContentIndexConfigDto_InitializesCorrectly()
    {
        // Arrange & Act
        var dto = new ContentIndexConfigDto
        {
            Type = "SqliteContentIndex",
            Path = "/db/path.db"
        };

        // Assert
        Assert.Equal("SqliteContentIndex", dto.Type);
        Assert.Equal("/db/path.db", dto.Path);
    }

    [Fact]
    public void NodeDetailsDto_InitializesCorrectly()
    {
        // Arrange & Act
        var dto = new NodeDetailsDto
        {
            NodeId = "node-1",
            Access = "ReadWrite",
            ContentIndex = new ContentIndexConfigDto { Type = "Sqlite", Path = "/db" },
            FileStorage = new StorageConfigDto { Type = "LocalDisk" },
            RepoStorage = new StorageConfigDto { Type = "Git" },
            SearchIndexes = new List<SearchIndexDto>
            {
                new() { Type = "Simple" }
            }
        };

        // Assert
        Assert.Equal("node-1", dto.NodeId);
        Assert.Equal("ReadWrite", dto.Access);
        Assert.NotNull(dto.ContentIndex);
        Assert.NotNull(dto.FileStorage);
        Assert.NotNull(dto.RepoStorage);
        Assert.Single(dto.SearchIndexes);
    }

    [Fact]
    public void NodeDetailsDto_WithNullOptionalFields_HandlesCorrectly()
    {
        // Arrange & Act
        var dto = new NodeDetailsDto
        {
            NodeId = "node-2",
            Access = "ReadOnly",
            ContentIndex = new ContentIndexConfigDto { Type = "Memory" },
            FileStorage = null,
            RepoStorage = null,
            SearchIndexes = new List<SearchIndexDto>()
        };

        // Assert
        Assert.Equal("node-2", dto.NodeId);
        Assert.Null(dto.FileStorage);
        Assert.Null(dto.RepoStorage);
        Assert.Empty(dto.SearchIndexes);
    }

    [Fact]
    public void NodeSummaryDto_InitializesCorrectly()
    {
        // Arrange & Act
        var dto = new NodeSummaryDto
        {
            Id = "summary-node",
            Access = "ReadWrite",
            ContentIndex = "SqliteContentIndex",
            HasFileStorage = true,
            HasRepoStorage = false,
            SearchIndexCount = 2
        };

        // Assert
        Assert.Equal("summary-node", dto.Id);
        Assert.Equal("ReadWrite", dto.Access);
        Assert.Equal("SqliteContentIndex", dto.ContentIndex);
        Assert.True(dto.HasFileStorage);
        Assert.False(dto.HasRepoStorage);
        Assert.Equal(2, dto.SearchIndexCount);
    }

    [Fact]
    public void SearchIndexDto_InitializesCorrectly()
    {
        // Arrange & Act
        var dto = new SearchIndexDto
        {
            Type = "SimpleSearch"
        };

        // Assert
        Assert.Equal("SimpleSearch", dto.Type);
    }

    [Fact]
    public void SearchIndexDto_WithNullType_HandlesCorrectly()
    {
        // Arrange & Act
        var dto = new SearchIndexDto
        {
            Type = null!
        };

        // Assert
        Assert.Null(dto.Type);
    }

    [Fact]
    public void StorageConfigDto_InitializesCorrectly()
    {
        // Arrange & Act
        var dto = new StorageConfigDto
        {
            Type = "AzureBlobStorage"
        };

        // Assert
        Assert.Equal("AzureBlobStorage", dto.Type);
    }

    [Fact]
    public void StorageConfigDto_WithNullType_HandlesCorrectly()
    {
        // Arrange & Act
        var dto = new StorageConfigDto
        {
            Type = null!
        };

        // Assert
        Assert.Null(dto.Type);
    }
}
