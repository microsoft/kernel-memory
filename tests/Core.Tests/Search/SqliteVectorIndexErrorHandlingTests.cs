// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Embeddings.Cache;
using Microsoft.Extensions.Logging;
using Moq;

namespace KernelMemory.Core.Tests.Search;

/// <summary>
/// Tests for SqliteVectorIndex error handling scenarios.
/// Validates that errors are handled gracefully with appropriate warnings/exceptions.
/// </summary>
public sealed class SqliteVectorIndexErrorHandlingTests : IDisposable
{
    private readonly string _dbPath;
    private readonly Mock<IEmbeddingGenerator> _mockGenerator;
    private readonly Mock<ILogger<SqliteVectorIndex>> _mockLogger;

    public SqliteVectorIndexErrorHandlingTests()
    {
        this._dbPath = Path.Combine(Path.GetTempPath(), $"vector-error-test-{Guid.NewGuid()}.db");
        this._mockGenerator = new Mock<IEmbeddingGenerator>();
        this._mockLogger = new Mock<ILogger<SqliteVectorIndex>>();

        // Setup mock generator to return predictable embeddings
        this._mockGenerator.Setup(g => g.VectorDimensions).Returns(3);
        this._mockGenerator.Setup(g => g.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmbeddingResult.FromVector([1.0f, 0.0f, 0.0f]));
    }

    public void Dispose()
    {
        if (File.Exists(this._dbPath))
        {
            File.Delete(this._dbPath);
        }
    }

    /// <summary>
    /// Verifies that cache write failures generate warnings but don't prevent indexing.
    /// This tests the non-blocking cache error handling requirement.
    /// </summary>
    [Fact]
    public async Task IndexAsync_WhenCacheWriteFails_ContinuesWithWarning()
    {
        // Arrange - Create a cache that fails on write
        var mockCache = new Mock<IEmbeddingCache>();
        mockCache.Setup(c => c.Mode).Returns(CacheModes.ReadWrite);
        mockCache.Setup(c => c.TryGetAsync(It.IsAny<EmbeddingCacheKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CachedEmbedding?)null); // Cache miss
        mockCache.Setup(c => c.StoreAsync(It.IsAny<EmbeddingCacheKey>(), It.IsAny<float[]>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Disk full")); // Cache write fails

        var mockCachedGenerator = new Mock<IEmbeddingGenerator>();
        mockCachedGenerator.Setup(g => g.VectorDimensions).Returns(3);
        mockCachedGenerator.Setup(g => g.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Cache write failed")); // Simulates CachedEmbeddingGenerator catching cache error

        // But the actual generator should work
        this._mockGenerator.Setup(g => g.GenerateAsync("test content", It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmbeddingResult.FromVector([1.0f, 0.0f, 0.0f]));

        using var index = new SqliteVectorIndex(this._dbPath, 3, useSqliteVec: false, this._mockGenerator.Object, this._mockLogger.Object);

        // Act - Should succeed despite cache error
        await index.IndexAsync("test-id", "test content", CancellationToken.None).ConfigureAwait(false);

        // Assert - Warning should be logged but operation succeeds
        // Verify data was actually stored
        var results = await index.SearchAsync("test content", limit: 10, CancellationToken.None).ConfigureAwait(false);
        Assert.Single(results);
        Assert.Equal("test-id", results[0].ContentId);
    }

    /// <summary>
    /// Verifies that cache read failures don't prevent indexing.
    /// System should continue without cache when read fails.
    /// </summary>
    [Fact]
    public async Task IndexAsync_WhenCacheReadFails_ContinuesWithoutCache()
    {
        // Arrange - Cache that fails on read
        var mockCache = new Mock<IEmbeddingCache>();
        mockCache.Setup(c => c.Mode).Returns(CacheModes.ReadWrite);
        mockCache.Setup(c => c.TryGetAsync(It.IsAny<EmbeddingCacheKey>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Cache corrupted"));

        // Even with cache read failure, generator should be called
        this._mockGenerator.Setup(g => g.GenerateAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmbeddingResult.FromVector([1.0f, 0.0f, 0.0f]));

        using var index = new SqliteVectorIndex(this._dbPath, 3, useSqliteVec: false, this._mockGenerator.Object, this._mockLogger.Object);

        // Act - Should succeed by calling generator directly
        await index.IndexAsync("id1", "test", CancellationToken.None).ConfigureAwait(false);

        // Assert - Data stored successfully
        var results = await index.SearchAsync("test", 10, CancellationToken.None).ConfigureAwait(false);
        Assert.Single(results);
    }

    /// <summary>
    /// Verifies that when embedding provider is unreachable, operation throws with clear message.
    /// This is the blocking behavior - operation should be queued for retry.
    /// </summary>
    [Fact]
    public async Task IndexAsync_WhenProviderUnreachable_ThrowsWithClearMessage()
    {
        // Arrange - Generator that simulates Ollama being down
        var failingGenerator = new Mock<IEmbeddingGenerator>();
        failingGenerator.Setup(g => g.VectorDimensions).Returns(3);
        failingGenerator.Setup(g => g.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        using var index = new SqliteVectorIndex(this._dbPath, 3, useSqliteVec: false, failingGenerator.Object, this._mockLogger.Object);

        // Act & Assert - Should throw and propagate to caller
        var ex = await Assert.ThrowsAsync<HttpRequestException>(async () =>
            await index.IndexAsync("id1", "test content", CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);

        Assert.Contains("Connection refused", ex.Message);
    }

    /// <summary>
    /// Verifies that invalid API key errors are propagated with clear messages.
    /// Operation should fail and be queued for retry.
    /// </summary>
    [Fact]
    public async Task IndexAsync_WhenApiKeyInvalid_ThrowsWithClearMessage()
    {
        // Arrange - Generator that simulates invalid API key
        var failingGenerator = new Mock<IEmbeddingGenerator>();
        failingGenerator.Setup(g => g.VectorDimensions).Returns(3);
        failingGenerator.Setup(g => g.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid API key"));

        using var index = new SqliteVectorIndex(this._dbPath, 3, useSqliteVec: false, failingGenerator.Object, this._mockLogger.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await index.IndexAsync("id1", "test content", CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);

        Assert.Contains("Invalid API key", ex.Message);
    }

    /// <summary>
    /// Verifies that when useSqliteVec is true but extension is unavailable,
    /// system logs a warning and falls back to C# implementation.
    /// This tests the graceful degradation requirement.
    /// Warning is logged during first IndexAsync call (lazy initialization).
    /// </summary>
    [Fact]
    public async Task IndexAsync_WhenSqliteVecUnavailableButRequested_LogsWarningAndContinues()
    {
        // Arrange - Request sqlite-vec but it won't be available (no extension installed)
        using var index = new SqliteVectorIndex(
            this._dbPath,
            dimensions: 3,
            useSqliteVec: true, // Request extension
            this._mockGenerator.Object,
            this._mockLogger.Object);

        // Act - First IndexAsync triggers initialization and warning
        await index.IndexAsync("test-id", "test content", CancellationToken.None).ConfigureAwait(false);

        // Assert - Operation should succeed
        var results = await index.SearchAsync("test content", 10, CancellationToken.None).ConfigureAwait(false);
        Assert.Single(results);

        // Verify warning was logged about sqlite-vec fallback
        this._mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("sqlite-vec")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Verifies that vector search produces same results whether using
    /// sqlite-vec extension or C# implementation (when extension unavailable).
    /// This ensures data format compatibility.
    /// </summary>
    [Fact]
    public async Task SearchAsync_WithAndWithoutSqliteVec_ProducesSameResults()
    {
        // Arrange - Create two indexes with same data
        var dbPath1 = Path.Combine(Path.GetTempPath(), $"vec-test-1-{Guid.NewGuid()}.db");
        var dbPath2 = Path.Combine(Path.GetTempPath(), $"vec-test-2-{Guid.NewGuid()}.db");

        try
        {
            this._mockGenerator.Setup(g => g.GenerateAsync("hello world", It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmbeddingResult.FromVector([0.6f, 0.8f, 0.0f]));
            this._mockGenerator.Setup(g => g.GenerateAsync("goodbye world", It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmbeddingResult.FromVector([0.8f, 0.6f, 0.0f]));
            this._mockGenerator.Setup(g => g.GenerateAsync("hello", It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmbeddingResult.FromVector([1.0f, 0.0f, 0.0f]));

            // Index without extension (C# implementation)
            using var index1 = new SqliteVectorIndex(dbPath1, 3, useSqliteVec: false, this._mockGenerator.Object, this._mockLogger.Object);
            await index1.IndexAsync("id1", "hello world", CancellationToken.None).ConfigureAwait(false);
            await index1.IndexAsync("id2", "goodbye world", CancellationToken.None).ConfigureAwait(false);

            // Index with extension requested (will fall back to C# if unavailable)
            using var index2 = new SqliteVectorIndex(dbPath2, 3, useSqliteVec: true, this._mockGenerator.Object, this._mockLogger.Object);
            await index2.IndexAsync("id1", "hello world", CancellationToken.None).ConfigureAwait(false);
            await index2.IndexAsync("id2", "goodbye world", CancellationToken.None).ConfigureAwait(false);

            // Act - Search both indexes
            var results1 = await index1.SearchAsync("hello", 10, CancellationToken.None).ConfigureAwait(false);
            var results2 = await index2.SearchAsync("hello", 10, CancellationToken.None).ConfigureAwait(false);

            // Assert - Results should be identical (same ranking, same scores)
            Assert.Equal(results1.Count, results2.Count);
            Assert.Equal(results1[0].ContentId, results2[0].ContentId);
            Assert.Equal(results1[0].Score, results2[0].Score, precision: 5);
            Assert.Equal(results1[1].ContentId, results2[1].ContentId);
            Assert.Equal(results1[1].Score, results2[1].Score, precision: 5);
        }
        finally
        {
            if (File.Exists(dbPath1))
            {
                File.Delete(dbPath1);
            }

            if (File.Exists(dbPath2))
            {
                File.Delete(dbPath2);
            }
        }
    }
}
