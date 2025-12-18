// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Core.Search.Models;
using KernelMemory.Core.Search.Query.Ast;
using KernelMemory.Core.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace KernelMemory.Core.Tests.Search;

/// <summary>
/// Unit tests for NodeSearchService index ID configuration.
/// Tests that index ID is properly configurable and not hardcoded.
/// Issue: Known Issue #4 - Index ID was hardcoded as "fts-main".
/// </summary>
public sealed class NodeSearchServiceIndexIdTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Mock<ILogger<SqliteFtsIndex>> _mockFtsLogger;
    private readonly Mock<ILogger<ContentStorageService>> _mockStorageLogger;
    private readonly List<ContentStorageDbContext> _contexts = [];
    private readonly List<SqliteFtsIndex> _ftsIndexes = [];

    public NodeSearchServiceIndexIdTests()
    {
        this._tempDir = Path.Combine(Path.GetTempPath(), $"km-index-id-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(this._tempDir);
        this._mockFtsLogger = new Mock<ILogger<SqliteFtsIndex>>();
        this._mockStorageLogger = new Mock<ILogger<ContentStorageService>>();
    }

    public void Dispose()
    {
        // Dispose all tracked resources
        foreach (var ftsIndex in this._ftsIndexes)
        {
            ftsIndex.Dispose();
        }

        foreach (var context in this._contexts)
        {
            context.Dispose();
        }

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

    /// <summary>
    /// Tests that NodeSearchService uses the provided index ID in search results.
    /// The index ID should be configurable, not hardcoded.
    /// </summary>
    [Fact]
    public async Task SearchAsync_WithCustomIndexId_ResultsContainThatIndexId()
    {
        // Arrange
        const string customIndexId = "custom-fts-index";
        const string nodeId = "test-node";

        var (ftsIndex, storage) = this.CreateIndexAndStorage("custom_index");

        // Insert content
        await storage.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Content = "Test content for custom index ID verification",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        // Create NodeSearchService with custom index ID
        var nodeService = new NodeSearchService(nodeId, ftsIndex, storage, customIndexId);

        var request = new SearchRequest
        {
            Query = "content",
            Limit = 10,
            MinRelevance = 0.0f
        };

        // Act - SearchIndexResult is returned by NodeSearchService.SearchAsync
        var queryNode = new TextSearchNode { SearchText = "content" };
        var (results, _) = await nodeService.SearchAsync(
            queryNode,
            request,
            CancellationToken.None).ConfigureAwait(false);

        // Assert - SearchIndexResult has IndexId property
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Equal(customIndexId, r.IndexId));
    }

    /// <summary>
    /// Tests that the default index ID constant is used when not specified.
    /// Ensures backward compatibility.
    /// </summary>
    [Fact]
    public async Task SearchAsync_WithDefaultIndexId_UsesSearchConstantsDefault()
    {
        // Arrange
        const string nodeId = "test-node";

        var (ftsIndex, storage) = this.CreateIndexAndStorage("default_index");

        // Insert content
        await storage.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Content = "Test content for default index ID verification",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        // Create NodeSearchService WITHOUT specifying index ID (uses default)
        var nodeService = new NodeSearchService(nodeId, ftsIndex, storage);

        var request = new SearchRequest
        {
            Query = "content",
            Limit = 10,
            MinRelevance = 0.0f
        };

        // Act
        var queryNode = new TextSearchNode { SearchText = "content" };
        var (results, _) = await nodeService.SearchAsync(
            queryNode,
            request,
            CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Equal(Constants.SearchDefaults.DefaultFtsIndexId, r.IndexId));
    }

    /// <summary>
    /// Tests that different nodes can have different index IDs.
    /// Validates multi-node, multi-index scenarios.
    /// </summary>
    [Fact]
    public async Task SearchAsync_MultipleNodesWithDifferentIndexIds_EachHasCorrectIndexId()
    {
        // Arrange
        const string node1Id = "node1";
        const string node1IndexId = "node1-fts-index";
        const string node2Id = "node2";
        const string node2IndexId = "node2-fts-index";

        var (ftsIndex1, storage1) = this.CreateIndexAndStorage("node1_db");
        var (ftsIndex2, storage2) = this.CreateIndexAndStorage("node2_db");

        // Insert content into both nodes
        await storage1.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Content = "Content in first node with custom index",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        await storage2.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Content = "Content in second node with different index",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        // Create NodeSearchServices with different index IDs
        var node1Service = new NodeSearchService(node1Id, ftsIndex1, storage1, node1IndexId);
        var node2Service = new NodeSearchService(node2Id, ftsIndex2, storage2, node2IndexId);

        var queryNode = new TextSearchNode { SearchText = "content" };
        var request = new SearchRequest
        {
            Query = "content",
            Limit = 10,
            MinRelevance = 0.0f
        };

        // Act - Test each node service directly
        var (results1, _) = await node1Service.SearchAsync(queryNode, request, CancellationToken.None).ConfigureAwait(false);
        var (results2, _) = await node2Service.SearchAsync(queryNode, request, CancellationToken.None).ConfigureAwait(false);

        // Assert - Each node returns results with its own index ID
        Assert.NotEmpty(results1);
        Assert.NotEmpty(results2);
        Assert.All(results1, r => Assert.Equal(node1IndexId, r.IndexId));
        Assert.All(results2, r => Assert.Equal(node2IndexId, r.IndexId));
    }

    /// <summary>
    /// Tests that Constants.SearchDefaults.DefaultFtsIndexId constant has the expected value.
    /// Validates the constant is properly defined.
    /// </summary>
    [Fact]
    public void DefaultFtsIndexId_HasExpectedValue()
    {
        // Assert
        Assert.Equal("fts-main", Constants.SearchDefaults.DefaultFtsIndexId);
    }

    /// <summary>
    /// Helper method to create FTS index and storage for testing.
    /// Tracks created resources for proper disposal.
    /// </summary>
    /// <param name="dbPrefix">Prefix for database file names to ensure uniqueness.</param>
    /// <returns>Tuple of FTS index and storage service.</returns>
    private (SqliteFtsIndex ftsIndex, ContentStorageService storage) CreateIndexAndStorage(string dbPrefix)
    {
        var ftsDbPath = Path.Combine(this._tempDir, $"{dbPrefix}_fts.db");
        var contentDbPath = Path.Combine(this._tempDir, $"{dbPrefix}_content.db");

        var ftsIndex = new SqliteFtsIndex(ftsDbPath, enableStemming: true, this._mockFtsLogger.Object);
        this._ftsIndexes.Add(ftsIndex);

        var options = new DbContextOptionsBuilder<ContentStorageDbContext>()
            .UseSqlite($"Data Source={contentDbPath}")
            .Options;
        var context = new ContentStorageDbContext(options);
        this._contexts.Add(context);
        context.Database.EnsureCreated();

        var cuidGenerator = new CuidGenerator();
        var searchIndexes = new Dictionary<string, ISearchIndex> { ["fts"] = ftsIndex };
        var storage = new ContentStorageService(context, cuidGenerator, this._mockStorageLogger.Object, (IReadOnlyDictionary<string, Core.Search.ISearchIndex>)searchIndexes);

        return (ftsIndex, storage);
    }
}
