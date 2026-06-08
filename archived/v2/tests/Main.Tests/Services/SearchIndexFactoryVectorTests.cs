// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Config.Embeddings;
using KernelMemory.Core.Config.SearchIndex;
using KernelMemory.Core.Search;
using KernelMemory.Main.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace KernelMemory.Main.Tests.Services;

/// <summary>
/// Unit tests for SearchIndexFactory vector index creation.
/// Tests verify correct index creation from configuration.
/// </summary>
public sealed class SearchIndexFactoryVectorTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly HttpClient _httpClient;

    public SearchIndexFactoryVectorTests()
    {
        this._tempDir = Path.Combine(Path.GetTempPath(), $"factory_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(this._tempDir);

        // Setup mock logger factory
        this._mockLoggerFactory = new Mock<ILoggerFactory>();
        this._mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        this._httpClient = new HttpClient();
    }

    public void Dispose()
    {
        this._httpClient.Dispose();

        // Clean up temp directory
        if (Directory.Exists(this._tempDir))
        {
            Directory.Delete(this._tempDir, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void CreateIndexesWithEmbeddings_CreatesFtsIndex()
    {
        // Arrange
        var ftsPath = Path.Combine(this._tempDir, "fts.db");
        var configs = new List<SearchIndexConfig>
        {
            new FtsSearchIndexConfig
            {
                Id = "fts-test",
                Path = ftsPath,
                EnableStemming = true
            }
        };

        // Act
        var indexes = SearchIndexFactory.CreateIndexes(
            configs,
            this._httpClient,
            embeddingCache: null,
            this._mockLoggerFactory.Object);

        // Assert
        Assert.Single(indexes);
        Assert.Contains("fts-test", indexes.Keys);
        Assert.IsType<SqliteFtsIndex>(indexes["fts-test"]);

        // Cleanup
        ((IDisposable)indexes["fts-test"]).Dispose();
    }

    [Fact]
    public void CreateIndexesWithEmbeddings_CreatesVectorIndex()
    {
        // Arrange
        var vectorPath = Path.Combine(this._tempDir, "vector.db");
        var configs = new List<SearchIndexConfig>
        {
            new VectorSearchIndexConfig
            {
                Id = "vector-test",
                Path = vectorPath,
                Dimensions = 384,
                UseSqliteVec = false,
                Embeddings = new OllamaEmbeddingsConfig
                {
                    Model = "test-model",
                    BaseUrl = "http://localhost:11434"
                }
            }
        };

        // Act
        var indexes = SearchIndexFactory.CreateIndexes(
            configs,
            this._httpClient,
            embeddingCache: null,
            this._mockLoggerFactory.Object);

        // Assert
        Assert.Single(indexes);
        Assert.Contains("vector-test", indexes.Keys);
        Assert.IsType<SqliteVectorIndex>(indexes["vector-test"]);

        // Verify dimensions
        var vectorIndex = (SqliteVectorIndex)indexes["vector-test"];
        Assert.Equal(384, vectorIndex.VectorDimensions);

        // Cleanup
        vectorIndex.Dispose();
    }

    [Fact]
    public void CreateIndexesWithEmbeddings_CreatesMixedIndexes()
    {
        // Arrange
        var ftsPath = Path.Combine(this._tempDir, "fts-mixed.db");
        var vectorPath = Path.Combine(this._tempDir, "vector-mixed.db");
        var configs = new List<SearchIndexConfig>
        {
            new FtsSearchIndexConfig
            {
                Id = "fts-mixed",
                Path = ftsPath,
                EnableStemming = true
            },
            new VectorSearchIndexConfig
            {
                Id = "vector-mixed",
                Path = vectorPath,
                Dimensions = 768,
                UseSqliteVec = false,
                Embeddings = new OllamaEmbeddingsConfig
                {
                    Model = "test-model",
                    BaseUrl = "http://localhost:11434"
                }
            }
        };

        // Act
        var indexes = SearchIndexFactory.CreateIndexes(
            configs,
            this._httpClient,
            embeddingCache: null,
            this._mockLoggerFactory.Object);

        // Assert
        Assert.Equal(2, indexes.Count);
        Assert.IsType<SqliteFtsIndex>(indexes["fts-mixed"]);
        Assert.IsType<SqliteVectorIndex>(indexes["vector-mixed"]);

        // Cleanup
        ((IDisposable)indexes["fts-mixed"]).Dispose();
        ((IDisposable)indexes["vector-mixed"]).Dispose();
    }

    [Fact]
    public void CreateIndexesWithEmbeddings_ThrowsForVectorIndexWithoutPath()
    {
        // Arrange
        var configs = new List<SearchIndexConfig>
        {
            new VectorSearchIndexConfig
            {
                Id = "vector-no-path",
                Path = null,
                Dimensions = 768,
                Embeddings = new OllamaEmbeddingsConfig
                {
                    Model = "test-model",
                    BaseUrl = "http://localhost:11434"
                }
            }
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            SearchIndexFactory.CreateIndexes(
                configs,
                this._httpClient,
                embeddingCache: null,
                this._mockLoggerFactory.Object));
    }

    [Fact]
    public void CreateIndexesWithEmbeddings_ThrowsForVectorIndexWithoutEmbeddings()
    {
        // Arrange
        var vectorPath = Path.Combine(this._tempDir, "vector-no-embeddings.db");
        var configs = new List<SearchIndexConfig>
        {
            new VectorSearchIndexConfig
            {
                Id = "vector-no-embeddings",
                Path = vectorPath,
                Dimensions = 768,
                Embeddings = null
            }
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            SearchIndexFactory.CreateIndexes(
                configs,
                this._httpClient,
                embeddingCache: null,
                this._mockLoggerFactory.Object));
    }

    [Fact]
    public void CreateIndexes_DoesNotCreateVectorIndexes()
    {
        // Arrange - Vector config should be ignored by CreateIndexes (no embedding support)
        var ftsPath = Path.Combine(this._tempDir, "fts-only.db");
        var vectorPath = Path.Combine(this._tempDir, "vector-ignored.db");
        var configs = new List<SearchIndexConfig>
        {
            new FtsSearchIndexConfig
            {
                Id = "fts-only",
                Path = ftsPath,
                EnableStemming = true
            },
            new VectorSearchIndexConfig
            {
                Id = "vector-ignored",
                Path = vectorPath,
                Dimensions = 768,
                Embeddings = new OllamaEmbeddingsConfig
                {
                    Model = "test-model",
                    BaseUrl = "http://localhost:11434"
                }
            }
        };

        // Act
        using var httpClient = new HttpClient();
        var indexes = SearchIndexFactory.CreateIndexes(
            configs,
            httpClient,
            embeddingCache: null,
            this._mockLoggerFactory.Object);

        // Assert - Both FTS and Vector indexes created
        Assert.Equal(2, indexes.Count);
        Assert.Contains("fts-only", indexes.Keys);
        Assert.Contains("vector-ignored", indexes.Keys);

        // Cleanup
        foreach (var index in indexes.Values.OfType<IDisposable>())
        {
            index.Dispose();
        }
    }
}
