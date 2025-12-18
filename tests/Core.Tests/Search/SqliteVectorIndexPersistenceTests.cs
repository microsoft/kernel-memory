// Copyright (c) Microsoft. All rights reserved.
using Microsoft.Extensions.Logging;
using Moq;

namespace KernelMemory.Core.Tests.Search;

/// <summary>
/// Tests for SqliteVectorIndex data persistence across dispose/recreate cycles.
/// Ensures vectors survive database reconnection.
/// </summary>
public sealed class SqliteVectorIndexPersistenceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly Mock<ILogger<SqliteVectorIndex>> _mockLogger;
    private readonly Mock<IEmbeddingGenerator> _mockEmbeddingGenerator;
    private const int TestDimensions = 4;

    public SqliteVectorIndexPersistenceTests()
    {
        // Use temp file for SQLite
        this._dbPath = Path.Combine(Path.GetTempPath(), $"vector_persist_test_{Guid.NewGuid()}.db");
        this._mockLogger = new Mock<ILogger<SqliteVectorIndex>>();
        this._mockEmbeddingGenerator = new Mock<IEmbeddingGenerator>();

        // Configure mock to return predictable embeddings
        this._mockEmbeddingGenerator
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string text, CancellationToken _) => EmbeddingResult.FromVector(this.GenerateTestEmbedding(text)));
    }

    public void Dispose()
    {
        // Clean up temp file
        if (File.Exists(this._dbPath))
        {
            File.Delete(this._dbPath);
        }

        // Clean up WAL files
        var walPath = this._dbPath + "-wal";
        var shmPath = this._dbPath + "-shm";
        if (File.Exists(walPath))
        {
            File.Delete(walPath);
        }

        if (File.Exists(shmPath))
        {
            File.Delete(shmPath);
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Generates a deterministic test embedding based on text hash.
    /// </summary>
    /// <param name="text"></param>
    private float[] GenerateTestEmbedding(string text)
    {
        var hash = text.GetHashCode();
        var embedding = new float[TestDimensions];
        for (int i = 0; i < TestDimensions; i++)
        {
            embedding[i] = ((hash >> (i * 8)) & 0xFF) / 255.0f;
        }

        return embedding;
    }

    [Fact]
    public async Task VectorsPersistAcrossDisposeAndRecreate()
    {
        // Arrange - Create and populate index
        const string contentId = "persist-test-1";
        const string text = "This is persisted content";

        using (var firstIndex = new SqliteVectorIndex(
            this._dbPath,
            TestDimensions,
            useSqliteVec: false,
            this._mockEmbeddingGenerator.Object,
            this._mockLogger.Object))
        {
            await firstIndex.IndexAsync(contentId, text).ConfigureAwait(false);

            // Verify it exists before dispose
            var beforeDispose = await firstIndex.SearchAsync(text).ConfigureAwait(false);
            Assert.Single(beforeDispose);
        }

        // Act - Create new index pointing to same file
        using (var secondIndex = new SqliteVectorIndex(
            this._dbPath,
            TestDimensions,
            useSqliteVec: false,
            this._mockEmbeddingGenerator.Object,
            this._mockLogger.Object))
        {
            // Assert - Data should persist
            var afterRecreate = await secondIndex.SearchAsync(text).ConfigureAwait(false);
            Assert.Single(afterRecreate);
            Assert.Equal(contentId, afterRecreate[0].ContentId);
        }
    }

    [Fact]
    public async Task MultipleVectorsPersistCorrectly()
    {
        // Arrange
        var testData = new Dictionary<string, string>
        {
            { "id1", "First document about science" },
            { "id2", "Second document about history" },
            { "id3", "Third document about mathematics" }
        };

        // Create and populate
        using (var firstIndex = new SqliteVectorIndex(
            this._dbPath,
            TestDimensions,
            useSqliteVec: false,
            this._mockEmbeddingGenerator.Object,
            this._mockLogger.Object))
        {
            foreach (var (id, content) in testData)
            {
                await firstIndex.IndexAsync(id, content).ConfigureAwait(false);
            }
        }

        // Act - Recreate and verify
        using (var secondIndex = new SqliteVectorIndex(
            this._dbPath,
            TestDimensions,
            useSqliteVec: false,
            this._mockEmbeddingGenerator.Object,
            this._mockLogger.Object))
        {
            var results = await secondIndex.SearchAsync("document").ConfigureAwait(false);

            // Assert
            Assert.Equal(3, results.Count);
            var contentIds = results.Select(r => r.ContentId).ToHashSet();
            Assert.Contains("id1", contentIds);
            Assert.Contains("id2", contentIds);
            Assert.Contains("id3", contentIds);
        }
    }

    [Fact]
    public async Task RemovalPersistsCorrectly()
    {
        // Arrange
        const string toKeep = "keep-id";
        const string toRemove = "remove-id";

        using (var firstIndex = new SqliteVectorIndex(
            this._dbPath,
            TestDimensions,
            useSqliteVec: false,
            this._mockEmbeddingGenerator.Object,
            this._mockLogger.Object))
        {
            await firstIndex.IndexAsync(toKeep, "Content to keep").ConfigureAwait(false);
            await firstIndex.IndexAsync(toRemove, "Content to remove").ConfigureAwait(false);
            await firstIndex.RemoveAsync(toRemove).ConfigureAwait(false);
        }

        // Act - Recreate and verify
        using (var secondIndex = new SqliteVectorIndex(
            this._dbPath,
            TestDimensions,
            useSqliteVec: false,
            this._mockEmbeddingGenerator.Object,
            this._mockLogger.Object))
        {
            var results = await secondIndex.SearchAsync("Content").ConfigureAwait(false);

            // Assert
            Assert.Single(results);
            Assert.Equal(toKeep, results[0].ContentId);
        }
    }

    [Fact]
    public async Task ClearPersistsCorrectly()
    {
        // Arrange
        using (var firstIndex = new SqliteVectorIndex(
            this._dbPath,
            TestDimensions,
            useSqliteVec: false,
            this._mockEmbeddingGenerator.Object,
            this._mockLogger.Object))
        {
            await firstIndex.IndexAsync("id1", "First content").ConfigureAwait(false);
            await firstIndex.IndexAsync("id2", "Second content").ConfigureAwait(false);
            await firstIndex.ClearAsync().ConfigureAwait(false);
        }

        // Act - Recreate and verify
        using (var secondIndex = new SqliteVectorIndex(
            this._dbPath,
            TestDimensions,
            useSqliteVec: false,
            this._mockEmbeddingGenerator.Object,
            this._mockLogger.Object))
        {
            var results = await secondIndex.SearchAsync("content").ConfigureAwait(false);

            // Assert
            Assert.Empty(results);
        }
    }

    [Fact]
    public async Task UpdatePersistsCorrectly()
    {
        // Arrange
        const string contentId = "update-test";

        using (var firstIndex = new SqliteVectorIndex(
            this._dbPath,
            TestDimensions,
            useSqliteVec: false,
            this._mockEmbeddingGenerator.Object,
            this._mockLogger.Object))
        {
            await firstIndex.IndexAsync(contentId, "Original content").ConfigureAwait(false);
            await firstIndex.IndexAsync(contentId, "Updated content").ConfigureAwait(false);
        }

        // Act - Recreate and verify
        using (var secondIndex = new SqliteVectorIndex(
            this._dbPath,
            TestDimensions,
            useSqliteVec: false,
            this._mockEmbeddingGenerator.Object,
            this._mockLogger.Object))
        {
            var results = await secondIndex.SearchAsync("Updated content").ConfigureAwait(false);

            // Assert - Should only have one entry for this ID
            Assert.Single(results);
            Assert.Equal(contentId, results[0].ContentId);
        }
    }

    [Fact]
    public async Task CanIndexAfterReopen()
    {
        // Arrange
        using (var firstIndex = new SqliteVectorIndex(
            this._dbPath,
            TestDimensions,
            useSqliteVec: false,
            this._mockEmbeddingGenerator.Object,
            this._mockLogger.Object))
        {
            await firstIndex.IndexAsync("id1", "First content").ConfigureAwait(false);
        }

        // Act - Reopen and add more content
        using (var secondIndex = new SqliteVectorIndex(
            this._dbPath,
            TestDimensions,
            useSqliteVec: false,
            this._mockEmbeddingGenerator.Object,
            this._mockLogger.Object))
        {
            await secondIndex.IndexAsync("id2", "Second content").ConfigureAwait(false);
            var results = await secondIndex.SearchAsync("content").ConfigureAwait(false);

            // Assert
            Assert.Equal(2, results.Count);
        }
    }
}
