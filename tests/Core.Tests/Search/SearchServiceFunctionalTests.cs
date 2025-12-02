// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Core.Search;
using KernelMemory.Core.Search.Models;
using KernelMemory.Core.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace KernelMemory.Core.Tests.Search;

/// <summary>
/// Functional tests for SearchService using real multi-node search.
/// Tests node filtering, ranking, and multi-node aggregation.
/// </summary>
public sealed class SearchServiceFunctionalTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SearchService _searchService;
    private readonly ContentStorageService _storage1;
    private readonly ContentStorageService _storage2;
    private readonly ContentStorageDbContext _context1;
    private readonly ContentStorageDbContext _context2;
    private readonly SqliteFtsIndex _fts1;
    private readonly SqliteFtsIndex _fts2;

    public SearchServiceFunctionalTests()
    {
        this._tempDir = Path.Combine(Path.GetTempPath(), $"km-search-service-func-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(this._tempDir);

        var mockStorageLogger1 = new Mock<ILogger<ContentStorageService>>();
        var mockStorageLogger2 = new Mock<ILogger<ContentStorageService>>();
        var mockFtsLogger1 = new Mock<ILogger<SqliteFtsIndex>>();
        var mockFtsLogger2 = new Mock<ILogger<SqliteFtsIndex>>();
        var cuidGenerator = new CuidGenerator();

        // Node 1
        var content1DbPath = Path.Combine(this._tempDir, "node1_content.db");
        var options1 = new DbContextOptionsBuilder<ContentStorageDbContext>()
            .UseSqlite($"Data Source={content1DbPath}")
            .Options;
        this._context1 = new ContentStorageDbContext(options1);
        this._context1.Database.EnsureCreated();

        var fts1DbPath = Path.Combine(this._tempDir, "node1_fts.db");
        this._fts1 = new SqliteFtsIndex(fts1DbPath, enableStemming: true, mockFtsLogger1.Object);
        var searchIndexes1 = new Dictionary<string, ISearchIndex> { ["fts"] = this._fts1 };
        this._storage1 = new ContentStorageService(this._context1, cuidGenerator, mockStorageLogger1.Object, searchIndexes1);
        var node1Service = new NodeSearchService("node1", this._fts1, this._storage1);

        // Node 2
        var content2DbPath = Path.Combine(this._tempDir, "node2_content.db");
        var options2 = new DbContextOptionsBuilder<ContentStorageDbContext>()
            .UseSqlite($"Data Source={content2DbPath}")
            .Options;
        this._context2 = new ContentStorageDbContext(options2);
        this._context2.Database.EnsureCreated();

        var fts2DbPath = Path.Combine(this._tempDir, "node2_fts.db");
        this._fts2 = new SqliteFtsIndex(fts2DbPath, enableStemming: true, mockFtsLogger2.Object);
        var searchIndexes2 = new Dictionary<string, ISearchIndex> { ["fts"] = this._fts2 };
        this._storage2 = new ContentStorageService(this._context2, cuidGenerator, mockStorageLogger2.Object, searchIndexes2);
        var node2Service = new NodeSearchService("node2", this._fts2, this._storage2);

        var nodeServices = new Dictionary<string, NodeSearchService>
        {
            ["node1"] = node1Service,
            ["node2"] = node2Service
        };

        this._searchService = new SearchService(nodeServices);
    }

    public void Dispose()
    {
        this._fts1.Dispose();
        this._fts2.Dispose();
        this._context1.Dispose();
        this._context2.Dispose();

        try
        {
            if (Directory.Exists(this._tempDir))
            {
                Directory.Delete(this._tempDir, true);
            }
        }
        catch (IOException)
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public async Task SearchAsync_AcrossMultipleNodes_AggregatesResults()
    {
        // Arrange: Insert into both nodes
        await this._storage1.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Content = "Docker tutorial from node1",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        await this._storage2.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Content = "Docker guide from node2",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        var request = new SearchRequest
        {
            Query = "docker",
            Limit = 10,
            MinRelevance = 0.0f
        };

        // Act
        var response = await this._searchService.SearchAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Results.Length >= 2);
        Assert.Equal(2, response.Metadata.NodesSearched);
        Assert.Contains(response.Results, r => r.NodeId == "node1");
        Assert.Contains(response.Results, r => r.NodeId == "node2");
    }

    [Fact]
    public async Task SearchAsync_WithNodeFilter_SearchesOnlySpecifiedNode()
    {
        // Arrange
        await this._storage1.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Content = "Content in node1",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        await this._storage2.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Content = "Content in node2",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        var request = new SearchRequest
        {
            Query = "content",
            Limit = 10,
            MinRelevance = 0.0f,
            Nodes = ["node1"]
        };

        // Act
        var response = await this._searchService.SearchAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(response);
        Assert.All(response.Results, r => Assert.Equal("node1", r.NodeId));
        Assert.Equal(1, response.Metadata.NodesSearched);
    }

    [Fact]
    public async Task SearchAsync_WithExcludeNodes_ExcludesSpecifiedNode()
    {
        // Arrange
        await this._storage1.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Content = "Kubernetes in node1",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        await this._storage2.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Content = "Kubernetes in node2",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        var request = new SearchRequest
        {
            Query = "kubernetes",
            Limit = 10,
            MinRelevance = 0.0f,
            ExcludeNodes = ["node2"]
        };

        // Act
        var response = await this._searchService.SearchAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(response);
        Assert.All(response.Results, r => Assert.NotEqual("node2", r.NodeId));
        Assert.Equal(1, response.Metadata.NodesSearched);
    }

    [Fact]
    public async Task ValidateQueryAsync_WithValidQuery_ReturnsValid()
    {
        // Act
        var result = await this._searchService.ValidateQueryAsync("kubernetes AND docker", CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.True(result.AvailableFields.Length > 0);
    }

    [Fact]
    public async Task ValidateQueryAsync_WithInvalidQuery_ReturnsInvalid()
    {
        // Act
        var result = await this._searchService.ValidateQueryAsync("kubernetes AND docker)", CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task SearchAsync_EmptyDatabase_ReturnsNoResults()
    {
        // Arrange
        var request = new SearchRequest
        {
            Query = "nonexistent",
            Limit = 10,
            MinRelevance = 0.0f
        };

        // Act
        var response = await this._searchService.SearchAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(response);
        Assert.Empty(response.Results);
        Assert.Equal(0, response.TotalResults);
    }
}
