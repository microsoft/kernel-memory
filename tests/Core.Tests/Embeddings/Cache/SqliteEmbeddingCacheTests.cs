// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Embeddings.Cache;
using Microsoft.Extensions.Logging;
using Moq;

namespace KernelMemory.Core.Tests.Embeddings.Cache;

/// <summary>
/// Tests for SqliteEmbeddingCache to verify CRUD operations, cache modes, and BLOB storage.
/// These tests use temporary SQLite databases to ensure isolation and avoid user data.
/// </summary>
public sealed class SqliteEmbeddingCacheTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly Mock<ILogger<SqliteEmbeddingCache>> _loggerMock;

    public SqliteEmbeddingCacheTests()
    {
        this._tempDbPath = Path.Combine(Path.GetTempPath(), $"test-cache-{Guid.NewGuid()}.db");
        this._loggerMock = new Mock<ILogger<SqliteEmbeddingCache>>();
    }

    public void Dispose()
    {
        // Clean up test database
        if (File.Exists(this._tempDbPath))
        {
            File.Delete(this._tempDbPath);
        }
    }

    [Fact]
    public async Task TryGetAsync_WithEmptyCache_ShouldReturnNull()
    {
        // Arrange
        using var cache = new SqliteEmbeddingCache(this._tempDbPath, CacheModes.ReadWrite, this._loggerMock.Object);
        var key = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "test text");

        // Act
        var result = await cache.TryGetAsync(key, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task StoreAsync_AndTryGetAsync_ShouldRoundTrip()
    {
        // Arrange
        using var cache = new SqliteEmbeddingCache(this._tempDbPath, CacheModes.ReadWrite, this._loggerMock.Object);
        var key = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "test text");
        var vector = new float[] { 0.1f, 0.2f, 0.3f, 0.4f };

        // Act
        await cache.StoreAsync(key, vector, tokenCount: null, CancellationToken.None).ConfigureAwait(false);
        var result = await cache.TryGetAsync(key, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(vector, result.Vector);
    }

    [Fact]
    public async Task StoreAsync_WithTokenCount_ShouldPreserveValue()
    {
        // Arrange
        using var cache = new SqliteEmbeddingCache(this._tempDbPath, CacheModes.ReadWrite, this._loggerMock.Object);
        var key = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "test text");
        var vector = new float[] { 0.1f, 0.2f, 0.3f };
        const int tokenCount = 42;

        // Act
        await cache.StoreAsync(key, vector, tokenCount, CancellationToken.None).ConfigureAwait(false);
        var result = await cache.TryGetAsync(key, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tokenCount, result.TokenCount);
    }

    [Fact]
    public async Task StoreAsync_ShouldSetTimestamp()
    {
        // Arrange
        using var cache = new SqliteEmbeddingCache(this._tempDbPath, CacheModes.ReadWrite, this._loggerMock.Object);
        var key = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "test text");
        var vector = new float[] { 0.1f, 0.2f, 0.3f };
        var beforeStore = DateTimeOffset.UtcNow;

        // Act
        await cache.StoreAsync(key, vector, tokenCount: null, CancellationToken.None).ConfigureAwait(false);
        var result = await cache.TryGetAsync(key, CancellationToken.None).ConfigureAwait(false);
        var afterStore = DateTimeOffset.UtcNow;

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Timestamp >= beforeStore.AddSeconds(-1)); // Allow 1 second tolerance
        Assert.True(result.Timestamp <= afterStore.AddSeconds(1));
    }

    [Fact]
    public async Task StoreAsync_WithLargeVector_ShouldRoundTrip()
    {
        // Arrange - 1536 dimensions (OpenAI ada-002)
        using var cache = new SqliteEmbeddingCache(this._tempDbPath, CacheModes.ReadWrite, this._loggerMock.Object);
        var key = EmbeddingCacheKey.Create("OpenAI", "text-embedding-ada-002", 1536, true, "test text");
        var vector = new float[1536];
        for (int i = 0; i < vector.Length; i++)
        {
            vector[i] = (float)i / 1536;
        }

        // Act
        await cache.StoreAsync(key, vector, tokenCount: null, CancellationToken.None).ConfigureAwait(false);
        var result = await cache.TryGetAsync(key, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1536, result.Vector.Length);
        for (int i = 0; i < vector.Length; i++)
        {
            Assert.Equal(vector[i], result.Vector[i]);
        }
    }

    [Fact]
    public async Task StoreAsync_WithSameKey_ShouldOverwrite()
    {
        // Arrange
        using var cache = new SqliteEmbeddingCache(this._tempDbPath, CacheModes.ReadWrite, this._loggerMock.Object);
        var key = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "test text");
        var vector1 = new float[] { 0.1f, 0.2f, 0.3f };
        var vector2 = new float[] { 0.9f, 0.8f, 0.7f };

        // Act
        await cache.StoreAsync(key, vector1, tokenCount: null, CancellationToken.None).ConfigureAwait(false);
        await cache.StoreAsync(key, vector2, tokenCount: null, CancellationToken.None).ConfigureAwait(false);
        var result = await cache.TryGetAsync(key, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(vector2, result.Vector);
    }

    [Fact]
    public async Task TryGetAsync_WithDifferentKeys_ShouldReturnCorrectValues()
    {
        // Arrange
        using var cache = new SqliteEmbeddingCache(this._tempDbPath, CacheModes.ReadWrite, this._loggerMock.Object);
        var key1 = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "text one");
        var key2 = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "text two");
        var vector1 = new float[] { 0.1f, 0.2f, 0.3f };
        var vector2 = new float[] { 0.4f, 0.5f, 0.6f };

        // Act
        await cache.StoreAsync(key1, vector1, tokenCount: null, CancellationToken.None).ConfigureAwait(false);
        await cache.StoreAsync(key2, vector2, tokenCount: null, CancellationToken.None).ConfigureAwait(false);
        var result1 = await cache.TryGetAsync(key1, CancellationToken.None).ConfigureAwait(false);
        var result2 = await cache.TryGetAsync(key2, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(vector1, result1.Vector);
        Assert.Equal(vector2, result2.Vector);
    }

    [Fact]
    public async Task ReadOnlyMode_TryGetAsync_ShouldWork()
    {
        // Arrange - First write with read-write mode
        using (var writeCache = new SqliteEmbeddingCache(this._tempDbPath, CacheModes.ReadWrite, this._loggerMock.Object))
        {
            var key = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "test text");
            var vector = new float[] { 0.1f, 0.2f, 0.3f };
            await writeCache.StoreAsync(key, vector, tokenCount: null, CancellationToken.None).ConfigureAwait(false);
        }

        // Act - Then read with read-only mode
        using var readCache = new SqliteEmbeddingCache(this._tempDbPath, CacheModes.ReadOnly, this._loggerMock.Object);
        var readKey = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "test text");
        var result = await readCache.TryGetAsync(readKey, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(new float[] { 0.1f, 0.2f, 0.3f }, result.Vector);
    }

    [Fact]
    public async Task ReadOnlyMode_StoreAsync_ShouldNotWrite()
    {
        // Arrange
        using var cache = new SqliteEmbeddingCache(this._tempDbPath, CacheModes.ReadOnly, this._loggerMock.Object);
        var key = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "test text");
        var vector = new float[] { 0.1f, 0.2f, 0.3f };

        // Act - Store should be ignored in read-only mode
        await cache.StoreAsync(key, vector, tokenCount: null, CancellationToken.None).ConfigureAwait(false);
        var result = await cache.TryGetAsync(key, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task WriteOnlyMode_StoreAsync_ShouldWork()
    {
        // Arrange
        using var cache = new SqliteEmbeddingCache(this._tempDbPath, CacheModes.WriteOnly, this._loggerMock.Object);
        var key = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "test text");
        var vector = new float[] { 0.1f, 0.2f, 0.3f };

        // Act
        await cache.StoreAsync(key, vector, tokenCount: null, CancellationToken.None).ConfigureAwait(false);

        // Assert - verify by reading with read-write cache
        using var readCache = new SqliteEmbeddingCache(this._tempDbPath, CacheModes.ReadWrite, this._loggerMock.Object);
        var result = await readCache.TryGetAsync(key, CancellationToken.None).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Equal(vector, result.Vector);
    }

    [Fact]
    public async Task WriteOnlyMode_TryGetAsync_ShouldReturnNull()
    {
        // Arrange - First store with read-write
        using (var writeCache = new SqliteEmbeddingCache(this._tempDbPath, CacheModes.ReadWrite, this._loggerMock.Object))
        {
            var key = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "test text");
            var vector = new float[] { 0.1f, 0.2f, 0.3f };
            await writeCache.StoreAsync(key, vector, tokenCount: null, CancellationToken.None).ConfigureAwait(false);
        }

        // Act - Read with write-only mode should return null
        using var cache = new SqliteEmbeddingCache(this._tempDbPath, CacheModes.WriteOnly, this._loggerMock.Object);
        var readKey = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "test text");
        var result = await cache.TryGetAsync(readKey, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Mode_ShouldReflectConstructorValue()
    {
        // Arrange & Act
        using var readWriteCache = new SqliteEmbeddingCache(this._tempDbPath, CacheModes.ReadWrite, this._loggerMock.Object);
        var path2 = Path.Combine(Path.GetTempPath(), $"test-cache-{Guid.NewGuid()}.db");
        using var readOnlyCache = new SqliteEmbeddingCache(path2, CacheModes.ReadOnly, this._loggerMock.Object);
        var path3 = Path.Combine(Path.GetTempPath(), $"test-cache-{Guid.NewGuid()}.db");
        using var writeOnlyCache = new SqliteEmbeddingCache(path3, CacheModes.WriteOnly, this._loggerMock.Object);

        try
        {
            // Assert
            Assert.Equal(CacheModes.ReadWrite, readWriteCache.Mode);
            Assert.Equal(CacheModes.ReadOnly, readOnlyCache.Mode);
            Assert.Equal(CacheModes.WriteOnly, writeOnlyCache.Mode);
        }
        finally
        {
            // Cleanup
            if (File.Exists(path2))
            {
                File.Delete(path2);
            }

            if (File.Exists(path3))
            {
                File.Delete(path3);
            }
        }
    }

    [Fact]
    public async Task VectorBlobStorage_ShouldPreserveFloatPrecision()
    {
        // Arrange
        using var cache = new SqliteEmbeddingCache(this._tempDbPath, CacheModes.ReadWrite, this._loggerMock.Object);
        var key = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "test text");

        // Include edge cases for float precision
        var vector = new float[]
        {
            0.123456789f,
            -0.987654321f,
            float.Epsilon,
            float.MaxValue / 2,
            float.MinValue / 2,
            0.0f,
            1.0f,
            -1.0f
        };

        // Act
        await cache.StoreAsync(key, vector, tokenCount: null, CancellationToken.None).ConfigureAwait(false);
        var result = await cache.TryGetAsync(key, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(result);
        for (int i = 0; i < vector.Length; i++)
        {
            Assert.Equal(vector[i], result.Vector[i]);
        }
    }

    [Fact]
    public async Task CacheDoesNotStoreInputText()
    {
        // Arrange
        using var cache = new SqliteEmbeddingCache(this._tempDbPath, CacheModes.ReadWrite, this._loggerMock.Object);
        const string sensitiveText = "This is sensitive PII data that should not be stored";
        var key = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, sensitiveText);
        var vector = new float[] { 0.1f, 0.2f, 0.3f };

        // Act
        await cache.StoreAsync(key, vector, tokenCount: null, CancellationToken.None).ConfigureAwait(false);

        // Assert - Read database file and verify text is not present
        var dbContent = await File.ReadAllBytesAsync(this._tempDbPath).ConfigureAwait(false);
        var dbString = System.Text.Encoding.UTF8.GetString(dbContent);
        Assert.DoesNotContain(sensitiveText, dbString);
    }

    [Fact]
    public async Task CachePersistence_ShouldSurviveReopen()
    {
        // Arrange
        var key = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "test text");
        var vector = new float[] { 0.1f, 0.2f, 0.3f };

        // Store and close
        using (var cache = new SqliteEmbeddingCache(this._tempDbPath, CacheModes.ReadWrite, this._loggerMock.Object))
        {
            await cache.StoreAsync(key, vector, tokenCount: null, CancellationToken.None).ConfigureAwait(false);
        }

        // Act - Reopen and read
        using var reopenedCache = new SqliteEmbeddingCache(this._tempDbPath, CacheModes.ReadWrite, this._loggerMock.Object);
        var result = await reopenedCache.TryGetAsync(key, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(vector, result.Vector);
    }

    [Fact]
    public async Task StoreAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        using var cache = new SqliteEmbeddingCache(this._tempDbPath, CacheModes.ReadWrite, this._loggerMock.Object);
        var key = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "test text");
        var vector = new float[] { 0.1f, 0.2f, 0.3f };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => cache.StoreAsync(key, vector, tokenCount: null, cts.Token)).ConfigureAwait(false);
    }

    [Fact]
    public async Task TryGetAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        using var cache = new SqliteEmbeddingCache(this._tempDbPath, CacheModes.ReadWrite, this._loggerMock.Object);
        var key = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "test text");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => cache.TryGetAsync(key, cts.Token)).ConfigureAwait(false);
    }
}
