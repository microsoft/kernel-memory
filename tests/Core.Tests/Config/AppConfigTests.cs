using KernelMemory.Core.Config;
using KernelMemory.Core.Config.Cache;
using KernelMemory.Core.Config.ContentIndex;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Config.SearchIndex;
using KernelMemory.Core.Config.Validation;

namespace Core.Tests.Config;

/// <summary>
/// Tests for AppConfig validation and default configuration
/// </summary>
public class AppConfigTests
{
    [Fact]
    public void CreateDefault_ShouldCreateValidConfiguration()
    {
        // Act
        var config = AppConfig.CreateDefault();

        // Assert
        Assert.NotNull(config);
        Assert.Single(config.Nodes);
        Assert.True(config.Nodes.ContainsKey("personal"));
        Assert.NotNull(config.EmbeddingsCache);
        Assert.Null(config.LLMCache);

        // Verify personal node structure
        var personalNode = config.Nodes["personal"];
        Assert.Equal("personal", personalNode.Id);
        Assert.Equal(NodeAccessLevels.Full, personalNode.Access);
        Assert.NotNull(personalNode.ContentIndex);
        Assert.IsType<SqliteContentIndexConfig>(personalNode.ContentIndex);
        Assert.Null(personalNode.FileStorage);
        Assert.Null(personalNode.RepoStorage);
        Assert.Equal(2, personalNode.SearchIndexes.Count);

        // Verify search indexes
        Assert.IsType<FtsSearchIndexConfig>(personalNode.SearchIndexes[0]);
        Assert.IsType<VectorSearchIndexConfig>(personalNode.SearchIndexes[1]);

        var ftsIndex = (FtsSearchIndexConfig)personalNode.SearchIndexes[0];
        Assert.Equal(SearchIndexTypes.SqliteFTS, ftsIndex.Type);
        Assert.True(ftsIndex.EnableStemming);
        Assert.NotNull(ftsIndex.Path);
        Assert.Contains("fts.db", ftsIndex.Path);

        var vectorIndex = (VectorSearchIndexConfig)personalNode.SearchIndexes[1];
        Assert.Equal(SearchIndexTypes.SqliteVector, vectorIndex.Type);
        Assert.Equal(768, vectorIndex.Dimensions);
        Assert.Equal(VectorMetrics.Cosine, vectorIndex.Metric);
        Assert.NotNull(vectorIndex.Path);
        Assert.Contains("vectors.db", vectorIndex.Path);

        // Verify embeddings cache
        Assert.Equal(CacheTypes.Sqlite, config.EmbeddingsCache.Type);
        Assert.True(config.EmbeddingsCache.AllowRead);
        Assert.True(config.EmbeddingsCache.AllowWrite);
        Assert.NotNull(config.EmbeddingsCache.Path);
        Assert.Contains("embeddings-cache.db", config.EmbeddingsCache.Path);
    }

    [Fact]
    public void Validate_WithValidConfig_ShouldNotThrow()
    {
        // Arrange
        var config = AppConfig.CreateDefault();

        // Act & Assert - should not throw
        config.Validate();
    }

    [Fact]
    public void Validate_WithNoNodes_ShouldThrowConfigException()
    {
        // Arrange
        var config = new AppConfig
        {
            Nodes = new Dictionary<string, NodeConfig>()
        };

        // Act & Assert
        var exception = Assert.Throws<ConfigException>(() => config.Validate());
        Assert.Equal("Nodes", exception.ConfigPath);
        Assert.Contains("At least one node must be configured", exception.Message);
    }

    [Fact]
    public void Validate_WithEmptyNodeId_ShouldThrowConfigException()
    {
        // Arrange
        var config = new AppConfig
        {
            Nodes = new Dictionary<string, NodeConfig>
            {
                [""] = new NodeConfig
                {
                    Id = "test",
                    ContentIndex = new SqliteContentIndexConfig { Path = "test.db" }
                }
            }
        };

        // Act & Assert
        var exception = Assert.Throws<ConfigException>(() => config.Validate());
        Assert.Equal("Nodes", exception.ConfigPath);
        Assert.Contains("Node ID cannot be empty", exception.Message);
    }

    [Fact]
    public void Validate_WithInvalidNodeConfig_ShouldThrowConfigException()
    {
        // Arrange
        var config = new AppConfig
        {
            Nodes = new Dictionary<string, NodeConfig>
            {
                ["test"] = new NodeConfig
                {
                    Id = "", // Invalid: empty ID
                    ContentIndex = new SqliteContentIndexConfig { Path = "test.db" }
                }
            }
        };

        // Act & Assert
        var exception = Assert.Throws<ConfigException>(() => config.Validate());
        Assert.Equal("Nodes.test", exception.ConfigPath);
        Assert.Contains("Node ID is required", exception.Message);
    }

    [Fact]
    public void Validate_WithInvalidEmbeddingsCache_ShouldThrowConfigException()
    {
        // Arrange
        var config = AppConfig.CreateDefault();
        config.EmbeddingsCache = new CacheConfig
        {
            Type = CacheTypes.Sqlite,
            Path = null // Invalid: sqlite needs path
        };

        // Act & Assert
        var exception = Assert.Throws<ConfigException>(() => config.Validate());
        Assert.Equal("EmbeddingsCache.Path", exception.ConfigPath);
        Assert.Contains("SQLite cache requires Path", exception.Message);
    }

    [Fact]
    public void Validate_WithInvalidLLMCache_ShouldThrowConfigException()
    {
        // Arrange
        var config = AppConfig.CreateDefault();
        config.LLMCache = new CacheConfig
        {
            Type = CacheTypes.Postgres,
            ConnectionString = null // Invalid: postgres needs connection string
        };

        // Act & Assert
        var exception = Assert.Throws<ConfigException>(() => config.Validate());
        Assert.Equal("LLMCache.ConnectionString", exception.ConfigPath);
        Assert.Contains("PostgreSQL cache requires ConnectionString", exception.Message);
    }

    [Fact]
    public void Validate_WithMultipleNodes_ShouldValidateAll()
    {
        // Arrange
        var config = AppConfig.CreateDefault();
        config.Nodes["work"] = new NodeConfig
        {
            Id = "work",
            Access = NodeAccessLevels.ReadOnly,
            ContentIndex = new SqliteContentIndexConfig { Path = "work.db" }
        };

        // Act & Assert - should not throw
        config.Validate();

        // Verify both nodes
        Assert.Equal(2, config.Nodes.Count);
        Assert.True(config.Nodes.ContainsKey("personal"));
        Assert.True(config.Nodes.ContainsKey("work"));
    }

    [Fact]
    public void Validate_PropagatesPathInErrors()
    {
        // Arrange
        var config = new AppConfig
        {
            Nodes = new Dictionary<string, NodeConfig>
            {
                ["mynode"] = new NodeConfig
                {
                    Id = "mynode",
                    ContentIndex = new SqliteContentIndexConfig { Path = "" } // Invalid: empty path
                }
            }
        };

        // Act & Assert
        var exception = Assert.Throws<ConfigException>(() => config.Validate());
        Assert.Equal("Nodes.mynode.ContentIndex.Path", exception.ConfigPath);
        Assert.Contains("SQLite path is required", exception.Message);
    }
}
