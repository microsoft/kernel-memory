using System.Text.Json;
using KernelMemory.Core.Config;
using KernelMemory.Core.Config.Validation;

namespace Core.Tests.Config;

/// <summary>
/// Tests for ConfigParser - loading and parsing configuration files
/// </summary>
public class ConfigParserTests
{
    [Fact]
    public void LoadFromFile_WhenFileMissing_ShouldReturnDefaultConfig()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}.json");

        // Act
        var config = ConfigParser.LoadFromFile(nonExistentPath);

        // Assert
        Assert.NotNull(config);
        Assert.Single(config.Nodes);
        Assert.True(config.Nodes.ContainsKey("personal"));
        Assert.NotNull(config.EmbeddingsCache);
    }

    [Fact]
    public void LoadFromFile_WithValidJson_ShouldReturnParsedConfig()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"config-{Guid.NewGuid()}.json");
        var json = @"{
            ""nodes"": {
                ""mynode"": {
                    ""id"": ""mynode"",
                    ""access"": ""Full"",
                    ""contentIndex"": {
                        ""$type"": ""sqlite"",
                        ""path"": ""test.db""
                    }
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
            Assert.Single(config.Nodes);
            Assert.True(config.Nodes.ContainsKey("mynode"));
            Assert.Equal("mynode", config.Nodes["mynode"].Id);
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
    public void LoadFromFile_WithInvalidJson_ShouldThrowConfigException()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"config-{Guid.NewGuid()}.json");
        var invalidJson = "{ invalid json here }";

        try
        {
            File.WriteAllText(tempFile, invalidJson);

            // Act & Assert
            var exception = Assert.Throws<ConfigException>(() => ConfigParser.LoadFromFile(tempFile));
            Assert.Contains("Failed to parse configuration", exception.Message);
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
    public void LoadFromFile_WithValidationErrors_ShouldThrowConfigException()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"config-{Guid.NewGuid()}.json");
        var json = @"{
            ""nodes"": {}
        }";

        try
        {
            File.WriteAllText(tempFile, json);

            // Act & Assert
            var exception = Assert.Throws<ConfigException>(() => ConfigParser.LoadFromFile(tempFile));
            Assert.Equal("Nodes", exception.ConfigPath);
            Assert.Contains("At least one node must be configured", exception.Message);
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
    public void LoadFromFile_WithTildeInPath_ShouldExpandToHomeDirectory()
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
                        ""path"": ""~/.km/test.db""
                    }
                }
            }
        }";

        try
        {
            File.WriteAllText(tempFile, json);

            // Act
            var config = ConfigParser.LoadFromFile(tempFile);

            // Assert
            var contentIndex = (KernelMemory.Core.Config.ContentIndex.SqliteContentIndexConfig)config.Nodes["test"].ContentIndex;
            Assert.NotNull(contentIndex.Path);
            Assert.DoesNotContain("~", contentIndex.Path);
            Assert.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), contentIndex.Path);
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
    public void LoadFromFile_WithCommentsInJson_ShouldParseSuccessfully()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"config-{Guid.NewGuid()}.json");
        var jsonWithComments = @"{
            // This is a comment
            ""nodes"": {
                ""test"": {
                    ""id"": ""test"",
                    /* Multi-line
                       comment */
                    ""access"": ""Full"",
                    ""contentIndex"": {
                        ""$type"": ""sqlite"",
                        ""path"": ""test.db""
                    }
                }
            }
        }";

        try
        {
            File.WriteAllText(tempFile, jsonWithComments);

            // Act
            var config = ConfigParser.LoadFromFile(tempFile);

            // Assert
            Assert.NotNull(config);
            Assert.Single(config.Nodes);
            Assert.True(config.Nodes.ContainsKey("test"));
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
    public void LoadFromFile_WithCaseInsensitiveProperties_ShouldParseSuccessfully()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"config-{Guid.NewGuid()}.json");
        var json = @"{
            ""Nodes"": {
                ""test"": {
                    ""Id"": ""test"",
                    ""Access"": ""Full"",
                    ""ContentIndex"": {
                        ""$type"": ""sqlite"",
                        ""Path"": ""test.db""
                    }
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
            Assert.Single(config.Nodes);
            Assert.Equal("test", config.Nodes["test"].Id);
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
    public void ParseFromString_WithValidJson_ShouldReturnParsedConfig()
    {
        // Arrange
        var json = @"{
            ""nodes"": {
                ""test"": {
                    ""id"": ""test"",
                    ""access"": ""Full"",
                    ""contentIndex"": {
                        ""$type"": ""sqlite"",
                        ""path"": ""test.db""
                    }
                }
            }
        }";

        // Act
        var config = ConfigParser.ParseFromString(json);

        // Assert
        Assert.NotNull(config);
        Assert.Single(config.Nodes);
        Assert.Equal("test", config.Nodes["test"].Id);
    }

    [Fact]
    public void ParseFromString_WithInvalidJson_ShouldThrowConfigException()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act & Assert
        var exception = Assert.Throws<ConfigException>(() => ConfigParser.ParseFromString(invalidJson));
        Assert.Contains("Failed to parse configuration", exception.Message);
    }

    [Fact]
    public void LoadFromFile_WithCacheTildeExpansion_ShouldExpandPaths()
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
                    }
                }
            },
            ""embeddingsCache"": {
                ""type"": ""Sqlite"",
                ""path"": ""~/embeddings-cache.db""
            },
            ""llmCache"": {
                ""type"": ""Sqlite"",
                ""path"": ""~/llm-cache.db""
            }
        }";

        try
        {
            File.WriteAllText(tempFile, json);

            // Act
            var config = ConfigParser.LoadFromFile(tempFile);

            // Assert
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            Assert.NotNull(config.EmbeddingsCache);
            Assert.NotNull(config.LLMCache);
            Assert.StartsWith(homeDir, config.EmbeddingsCache.Path!);
            Assert.StartsWith(homeDir, config.LLMCache.Path!);
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
    public void LoadFromFile_WithCacheBothPathAndConnectionString_ShouldThrowConfigException()
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
                    }
                }
            },
            ""embeddingsCache"": {
                ""type"": ""Sqlite"",
                ""path"": ""cache.db"",
                ""connectionString"": ""Host=localhost""
            }
        }";

        try
        {
            File.WriteAllText(tempFile, json);

            // Act & Assert
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
}
