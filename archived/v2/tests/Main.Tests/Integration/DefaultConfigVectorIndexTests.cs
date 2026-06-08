// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Core.Config;
using KernelMemory.Core.Config.ContentIndex;
using KernelMemory.Core.Config.Embeddings;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Config.SearchIndex;
using KernelMemory.Core.Search;
using KernelMemory.Core.Storage;
using KernelMemory.Core.Storage.Models;
using KernelMemory.Main.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KernelMemory.Main.Tests.Integration;

/// <summary>
/// Integration tests verifying that the default configuration includes
/// vector search indexes and that all configured indexes are properly created
/// and used during ingestion operations.
/// </summary>
public sealed class DefaultConfigVectorIndexTests
{
    /// <summary>
    /// Verifies that the default configuration includes both FTS and vector search indexes
    /// as specified in Feature 00001.
    /// This is a regression test to catch if vector indexes are accidentally removed from defaults.
    /// </summary>
    [Fact]
    public void DefaultConfig_ShouldIncludeBothFtsAndVectorIndexes()
    {
        // Arrange & Act
        var config = AppConfig.CreateDefault("/tmp/test");

        // Assert
        var personalNode = config.Nodes["personal"];
        Assert.NotNull(personalNode);
        Assert.Equal(2, personalNode.SearchIndexes.Count);

        // Verify FTS index exists
        var ftsIndex = personalNode.SearchIndexes.FirstOrDefault(i => i is FtsSearchIndexConfig);
        Assert.NotNull(ftsIndex);
        Assert.Equal("sqlite-fts", ftsIndex.Id);
        Assert.True(ftsIndex.Required); // FTS should be required

        // Verify Vector index exists
        var vectorIndex = personalNode.SearchIndexes.FirstOrDefault(i => i is VectorSearchIndexConfig) as VectorSearchIndexConfig;
        Assert.NotNull(vectorIndex);
        Assert.Equal("sqlite-vector", vectorIndex.Id);
        Assert.False(vectorIndex.Required); // Vector should be optional (Ollama may not be running)
        Assert.Equal(1024, vectorIndex.Dimensions);
        Assert.False(vectorIndex.UseSqliteVec);

        // Verify vector index has Ollama embeddings configured
        Assert.NotNull(vectorIndex.Embeddings);
        Assert.IsType<OllamaEmbeddingsConfig>(vectorIndex.Embeddings);
        var ollamaConfig = (OllamaEmbeddingsConfig)vectorIndex.Embeddings;
        Assert.Equal("qwen3-embedding:0.6b", ollamaConfig.Model);
        Assert.Equal("http://localhost:11434", ollamaConfig.BaseUrl);
    }

    /// <summary>
    /// Verifies that the default configuration includes embeddings cache
    /// as specified in Feature 00001.
    /// </summary>
    [Fact]
    public void DefaultConfig_ShouldIncludeEmbeddingsCache()
    {
        // Arrange & Act
        var config = AppConfig.CreateDefault("/tmp/test");

        // Assert
        Assert.NotNull(config.EmbeddingsCache);
        Assert.Equal("/tmp/test/embeddings-cache.db", config.EmbeddingsCache.Path);
        Assert.True(config.EmbeddingsCache.AllowRead);
        Assert.True(config.EmbeddingsCache.AllowWrite);
    }

    /// <summary>
    /// Verifies that when a node has multiple search indexes (20+ mixed types),
    /// ALL indexes are created and registered for use during ingestion.
    /// This is a critical regression test ensuring no indexes are skipped.
    /// </summary>
    [Fact]
    public void CreateIndexes_WithManyMixedIndexTypes_ShouldCreateAllIndexes()
    {
        // Arrange - Create config with 20 indexes (mix of FTS and Vector)
        var configs = new List<SearchIndexConfig>();

        // Add 10 FTS indexes
        for (int i = 0; i < 10; i++)
        {
            configs.Add(new FtsSearchIndexConfig
            {
                Id = $"fts-{i}",
                Type = SearchIndexTypes.SqliteFTS,
                Path = $"/tmp/test/fts-{i}.db",
                EnableStemming = i % 2 == 0 // Alternate stemming
            });
        }

        // Add 10 Vector indexes with different dimensions
        var dimensions = new[] { 384, 768, 1024, 1536, 3072 };
        for (int i = 0; i < 10; i++)
        {
            configs.Add(new VectorSearchIndexConfig
            {
                Id = $"vector-{i}",
                Type = SearchIndexTypes.SqliteVector,
                Path = $"/tmp/test/vector-{i}.db",
                Dimensions = dimensions[i % dimensions.Length],
                UseSqliteVec = false,
                Embeddings = new OllamaEmbeddingsConfig
                {
                    Model = "qwen3-embedding",
                    BaseUrl = "http://localhost:11434"
                }
            });
        }

        // Act
        using var httpClient = new HttpClient();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var indexes = SearchIndexFactory.CreateIndexes(configs, httpClient, null, loggerFactory);

        // Assert - ALL 20 indexes should be created
        Assert.Equal(20, indexes.Count);

        // Verify all FTS indexes present
        for (int i = 0; i < 10; i++)
        {
            Assert.Contains($"fts-{i}", indexes.Keys);
            Assert.IsAssignableFrom<IFtsIndex>(indexes[$"fts-{i}"]);
        }

        // Verify all Vector indexes present
        for (int i = 0; i < 10; i++)
        {
            Assert.Contains($"vector-{i}", indexes.Keys);
            Assert.IsAssignableFrom<IVectorIndex>(indexes[$"vector-{i}"]);
        }

        // Cleanup
        foreach (var index in indexes.Values.OfType<IDisposable>())
        {
            index.Dispose();
        }
    }

    /// <summary>
    /// Verifies that upsert operations create steps for ALL configured indexes.
    /// This ensures the ingestion pipeline will update every index during km put.
    /// </summary>
    [Fact]
    public async Task UpsertOperation_WithMultipleIndexes_ShouldCreateStepsForAllIndexes()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"km-test-{Guid.NewGuid()}");
        var nodeDir = Path.Combine(tempDir, "nodes", "multi");
        Directory.CreateDirectory(nodeDir);

        try
        {
            // Create config with 3 search indexes
            var config = new AppConfig
            {
                Nodes = new Dictionary<string, NodeConfig>
                {
                    ["multi"] = new NodeConfig
                    {
                        Id = "multi",
                        Access = NodeAccessLevels.Full,
                        ContentIndex = new SqliteContentIndexConfig
                        {
                            Path = Path.Combine(nodeDir, "content.db")
                        },
                        SearchIndexes = new List<SearchIndexConfig>
                        {
                            new FtsSearchIndexConfig
                            {
                                Id = "fts-1",
                                Type = SearchIndexTypes.SqliteFTS,
                                Path = Path.Combine(nodeDir, "fts-1.db"),
                                EnableStemming = true
                            },
                            new FtsSearchIndexConfig
                            {
                                Id = "fts-2",
                                Type = SearchIndexTypes.SqliteFTS,
                                Path = Path.Combine(nodeDir, "fts-2.db"),
                                EnableStemming = false
                            },
                            new VectorSearchIndexConfig
                            {
                                Id = "vector-1",
                                Type = SearchIndexTypes.SqliteVector,
                                Path = Path.Combine(nodeDir, "vector-1.db"),
                                Dimensions = 1024,
                                UseSqliteVec = false,
                                Embeddings = new OllamaEmbeddingsConfig
                                {
                                    Model = "qwen3-embedding",
                                    BaseUrl = "http://localhost:11434"
                                }
                            }
                        }
                    }
                }
            };

            // Create content storage service with all indexes
            var connectionString = "Data Source=" + Path.Combine(nodeDir, "content.db");
            var optionsBuilder = new DbContextOptionsBuilder<ContentStorageDbContext>();
            optionsBuilder.UseSqlite(connectionString);
            var context = new ContentStorageDbContext(optionsBuilder.Options);
            context.Database.EnsureCreated();

            var cuidGenerator = new CuidGenerator();
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            var storageLogger = loggerFactory.CreateLogger<ContentStorageService>();

            using var httpClient = new HttpClient();
            var indexes = SearchIndexFactory.CreateIndexes(
                config.Nodes["multi"].SearchIndexes,
                httpClient,
                embeddingCache: null,
                loggerFactory);

            var storage = new ContentStorageService(context, cuidGenerator, storageLogger, indexes);

            // Act - Queue an upsert operation
            var request = new UpsertRequest
            {
                Content = "Test content for multi-index ingestion",
                MimeType = "text/plain"
            };

            var result = await storage.UpsertAsync(request, CancellationToken.None).ConfigureAwait(false);

            // Assert - Verify operation was queued with steps for ALL 3 indexes
            var operation = await context.Operations
                .FirstOrDefaultAsync(o => o.ContentId == result.Id).ConfigureAwait(false);

            Assert.NotNull(operation);
            Assert.Contains("upsert", operation.PlannedSteps);
            Assert.Contains("index:fts-1", operation.PlannedSteps);
            Assert.Contains("index:fts-2", operation.PlannedSteps);
            Assert.Contains("index:vector-1", operation.PlannedSteps);
            Assert.Equal(4, operation.PlannedSteps.Length); // upsert + 3 index steps

            // Cleanup
            foreach (var index in indexes.Values.OfType<IDisposable>())
            {
                index.Dispose();
            }
            context.Dispose();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
