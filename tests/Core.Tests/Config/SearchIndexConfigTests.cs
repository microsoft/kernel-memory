using KernelMemory.Core.Config;
using KernelMemory.Core.Config.Validation;

namespace Core.Tests.Config;

/// <summary>
/// Tests for Search Index configuration validation
/// </summary>
internal sealed class SearchIndexConfigTests
{
    [Fact]
    public void LoadFromFile_WithGraphSearchIndex_ShouldExpandTildePath()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"config-{Guid.NewGuid()}.json");
        var json = @"{
            ""nodes"": {
                ""test"": {
                    ""id"": ""test"",
                    ""access"": ""Full"",
                    ""contentIndex"": {
                        ""$type"": ""sqlite"",
                        ""path"": ""test.db""
                    },
                    ""searchIndexes"": [
                        {
                            ""$type"": ""graph"",
                            ""path"": ""~/graph-index.db""
                        }
                    ]
                }
            }
        }";

        try
        {
            File.WriteAllText(tempFile, json);

            // Act
            var config = ConfigParser.LoadFromFile(tempFile);

            // Assert
            Assert.NotNull(config);
            var node = config.Nodes["test"];
            Assert.Single(node.SearchIndexes);
            var graphIndex = Assert.IsType<KernelMemory.Core.Config.SearchIndex.GraphSearchIndexConfig>(node.SearchIndexes[0]);

            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            Assert.StartsWith(homeDir, graphIndex.Path);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void LoadFromFile_WithFtsSearchIndexBothPathAndConnection_ShouldThrowConfigException()
    {
        // Test: FTS with both Path and ConnectionString
        var tempFile = Path.Combine(Path.GetTempPath(), $"config-{Guid.NewGuid()}.json");
        var json = @"{
            ""nodes"": {
                ""test"": {
                    ""id"": ""test"",
                    ""access"": ""Full"",
                    ""contentIndex"": {
                        ""$type"": ""sqlite"",
                        ""path"": ""test.db""
                    },
                    ""searchIndexes"": [
                        {
                            ""$type"": ""fts"",
                            ""type"": ""SqliteFTS"",
                            ""path"": ""fts.db"",
                            ""connectionString"": ""Host=localhost""
                        }
                    ]
                }
            }
        }";

        try
        {
            File.WriteAllText(tempFile, json);
            var exception = Assert.Throws<ConfigException>(() => ConfigParser.LoadFromFile(tempFile));
            Assert.Contains("specify either Path", exception.Message);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void LoadFromFile_WithVectorSearchIndexBothPathAndConnection_ShouldThrowConfigException()
    {
        // Test: Vector with both Path and ConnectionString
        var tempFile = Path.Combine(Path.GetTempPath(), $"config-{Guid.NewGuid()}.json");
        var json = @"{
            ""nodes"": {
                ""test"": {
                    ""id"": ""test"",
                    ""access"": ""Full"",
                    ""contentIndex"": {
                        ""$type"": ""sqlite"",
                        ""path"": ""test.db""
                    },
                    ""searchIndexes"": [
                        {
                            ""$type"": ""vector"",
                            ""type"": ""SqliteVector"",
                            ""path"": ""vector.db"",
                            ""connectionString"": ""Host=localhost"",
                            ""dimensions"": 384
                        }
                    ]
                }
            }
        }";

        try
        {
            File.WriteAllText(tempFile, json);
            var exception = Assert.Throws<ConfigException>(() => ConfigParser.LoadFromFile(tempFile));
            Assert.Contains("specify either Path", exception.Message);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void LoadFromFile_WithVectorSearchIndexInvalidDimensions_ShouldThrowConfigException()
    {
        // Test: Vector with invalid Dimensions
        var tempFile = Path.Combine(Path.GetTempPath(), $"config-{Guid.NewGuid()}.json");
        var json = @"{
            ""nodes"": {
                ""test"": {
                    ""id"": ""test"",
                    ""access"": ""Full"",
                    ""contentIndex"": {
                        ""$type"": ""sqlite"",
                        ""path"": ""test.db""
                    },
                    ""searchIndexes"": [
                        {
                            ""$type"": ""vector"",
                            ""type"": ""SqliteVector"",
                            ""path"": ""vector.db"",
                            ""dimensions"": 0
                        }
                    ]
                }
            }
        }";

        try
        {
            File.WriteAllText(tempFile, json);
            var exception = Assert.Throws<ConfigException>(() => ConfigParser.LoadFromFile(tempFile));
            Assert.Contains("must be positive", exception.Message);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
