// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Core.Search.Models;
using KernelMemory.Core.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace KernelMemory.Core.Tests.Search;

/// <summary>
/// Tests for SearchService with configurable index weights.
/// Verifies that index weights from configuration are used in reranking.
/// This test class tests the fix for Known Issue 4: Index Weights Not Configurable.
/// </summary>
public sealed class SearchServiceIndexWeightsTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ContentStorageService _storage;
    private readonly ContentStorageDbContext _context;
    private readonly SqliteFtsIndex _ftsIndex;

    public SearchServiceIndexWeightsTests()
    {
        this._tempDir = Path.Combine(Path.GetTempPath(), $"km-index-weights-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(this._tempDir);

        var mockStorageLogger = new Mock<ILogger<ContentStorageService>>();
        var mockFtsLogger = new Mock<ILogger<SqliteFtsIndex>>();
        var cuidGenerator = new CuidGenerator();

        // Create storage
        var contentDbPath = Path.Combine(this._tempDir, "content.db");
        var options = new DbContextOptionsBuilder<ContentStorageDbContext>()
            .UseSqlite($"Data Source={contentDbPath}")
            .Options;
        this._context = new ContentStorageDbContext(options);
        this._context.Database.EnsureCreated();

        // Create FTS index
        var ftsDbPath = Path.Combine(this._tempDir, "fts.db");
        this._ftsIndex = new SqliteFtsIndex(ftsDbPath, enableStemming: true, mockFtsLogger.Object);

        var searchIndexes = new Dictionary<string, ISearchIndex> { ["fts"] = this._ftsIndex };
        this._storage = new ContentStorageService(this._context, cuidGenerator, mockStorageLogger.Object, (IReadOnlyDictionary<string, Core.Search.ISearchIndex>)searchIndexes);
    }

    public void Dispose()
    {
        this._ftsIndex.Dispose();
        this._context.Dispose();

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
    public async Task SearchService_WithConfiguredIndexWeights_UsesConfiguredWeights()
    {
        // Arrange: Insert test data
        await this._storage.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Content = "Test document about Docker containers",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        // Configure custom index weights (0.5 instead of default 1.0)
        var indexWeights = new Dictionary<string, Dictionary<string, float>>
        {
            ["test-node"] = new Dictionary<string, float>
            {
                [Constants.SearchDefaults.DefaultFtsIndexId] = 0.5f  // Custom weight
            }
        };

        var nodeService = new NodeSearchService("test-node", this._ftsIndex, this._storage);
        var nodeServices = new Dictionary<string, NodeSearchService> { ["test-node"] = nodeService };

        // Create SearchService with custom index weights
        var searchService = new SearchService(nodeServices, indexWeights: indexWeights);

        var request = new SearchRequest
        {
            Query = "docker",
            Limit = 10,
            MinRelevance = 0.0f
        };

        // Act
        var response = await searchService.SearchAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotEmpty(response.Results);
        // With default weight 1.0, a result with BaseRelevance ~1.0 would have Relevance ~1.0
        // With weight 0.5, the same result should have Relevance ~0.5
        // Since we set index weight to 0.5, relevance should be reduced by factor of 0.5
        var result = response.Results.First();
        Assert.True(result.Relevance <= 0.6f,
            $"Expected relevance <= 0.6 (due to 0.5 index weight), got {result.Relevance}");
    }

    [Fact]
    public async Task SearchService_WithoutConfiguredIndexWeights_UsesDefaultWeight()
    {
        // Arrange: Insert test data
        await this._storage.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Content = "Test document about Kubernetes orchestration",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        var nodeService = new NodeSearchService("test-node", this._ftsIndex, this._storage);
        var nodeServices = new Dictionary<string, NodeSearchService> { ["test-node"] = nodeService };

        // Create SearchService WITHOUT custom index weights (should use defaults)
        var searchService = new SearchService(nodeServices);

        var request = new SearchRequest
        {
            Query = "kubernetes",
            Limit = 10,
            MinRelevance = 0.0f
        };

        // Act
        var response = await searchService.SearchAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotEmpty(response.Results);
        // With default weight 1.0, result should maintain its base relevance
        var result = response.Results.First();
        Assert.True(result.Relevance > 0.5f,
            $"Expected relevance > 0.5 (using default 1.0 weight), got {result.Relevance}");
    }

    [Fact]
    public async Task SearchService_WithIndexWeightsForMultipleIndexes_UsesCorrectWeights()
    {
        // This test verifies that when index weights are configured for multiple indexes,
        // each index uses its correct weight for reranking.

        // Arrange: Insert test data
        await this._storage.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Content = "Machine learning and artificial intelligence concepts",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        // Configure weights for multiple indexes
        var indexWeights = new Dictionary<string, Dictionary<string, float>>
        {
            ["test-node"] = new Dictionary<string, float>
            {
                [Constants.SearchDefaults.DefaultFtsIndexId] = 0.7f,  // FTS index weight
                ["vector-main"] = 0.3f  // Vector index weight (not used here, but configured)
            }
        };

        var nodeService = new NodeSearchService("test-node", this._ftsIndex, this._storage);
        var nodeServices = new Dictionary<string, NodeSearchService> { ["test-node"] = nodeService };

        var searchService = new SearchService(nodeServices, indexWeights: indexWeights);

        var request = new SearchRequest
        {
            Query = "machine learning",
            Limit = 10,
            MinRelevance = 0.0f
        };

        // Act
        var response = await searchService.SearchAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotEmpty(response.Results);
        // With index weight 0.7 (not default 1.0), relevance should be scaled by 0.7
        var result = response.Results.First();
        Assert.True(result.Relevance <= 0.75f,
            $"Expected relevance <= 0.75 (due to 0.7 index weight), got {result.Relevance}");
    }

    [Fact]
    public async Task SearchService_WithMissingIndexWeight_UsesDefaultWeight()
    {
        // Tests that if an index weight is not configured for a specific index,
        // the default weight is used.

        // Arrange: Insert test data
        await this._storage.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Content = "Database optimization techniques",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        // Configure weights but NOT for the FTS index used
        var indexWeights = new Dictionary<string, Dictionary<string, float>>
        {
            ["test-node"] = new Dictionary<string, float>
            {
                ["some-other-index"] = 0.5f  // Different index, not the one used
            }
        };

        var nodeService = new NodeSearchService("test-node", this._ftsIndex, this._storage);
        var nodeServices = new Dictionary<string, NodeSearchService> { ["test-node"] = nodeService };

        var searchService = new SearchService(nodeServices, indexWeights: indexWeights);

        var request = new SearchRequest
        {
            Query = "database",
            Limit = 10,
            MinRelevance = 0.0f
        };

        // Act
        var response = await searchService.SearchAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotEmpty(response.Results);
        // Since fts-main is not in the configured weights, default weight 1.0 should be used
        var result = response.Results.First();
        Assert.True(result.Relevance > 0.5f,
            $"Expected relevance > 0.5 (using default 1.0 weight for unconfigured index), got {result.Relevance}");
    }

    [Fact]
    public void SearchService_Constructor_AcceptsNullIndexWeights()
    {
        // Verify that SearchService can be created with null index weights
        var nodeService = new NodeSearchService("test-node", this._ftsIndex, this._storage);
        var nodeServices = new Dictionary<string, NodeSearchService> { ["test-node"] = nodeService };

        // Act - should not throw
        var searchService = new SearchService(nodeServices, indexWeights: null);

        // Assert - service was created successfully
        Assert.NotNull(searchService);
    }

    [Fact]
    public void SearchService_Constructor_AcceptsEmptyIndexWeights()
    {
        // Verify that SearchService works with empty index weights dictionary
        var nodeService = new NodeSearchService("test-node", this._ftsIndex, this._storage);
        var nodeServices = new Dictionary<string, NodeSearchService> { ["test-node"] = nodeService };

        var emptyWeights = new Dictionary<string, Dictionary<string, float>>();

        // Act - should not throw
        var searchService = new SearchService(nodeServices, indexWeights: emptyWeights);

        // Assert - service was created successfully
        Assert.NotNull(searchService);
    }
}
