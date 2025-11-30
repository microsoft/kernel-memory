// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Core.Config;
using KernelMemory.Core.Config.ContentIndex;
using KernelMemory.Core.Config.SearchIndex;
using Xunit;

namespace KernelMemory.Core.Tests.Config;

/// <summary>
/// Tests that ConfigParser automatically creates config files when they don't exist
/// </summary>
public sealed class ConfigParserAutoCreateTests : IDisposable
{
    private readonly string _tempDir;

    public ConfigParserAutoCreateTests()
    {
        this._tempDir = Path.Combine(Path.GetTempPath(), $"km-autoconfig-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(this._tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(this._tempDir))
            {
                Directory.Delete(this._tempDir, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public void LoadFromFile_WhenFileDoesNotExist_CreatesConfigFile()
    {
        // Arrange
        var configPath = Path.Combine(this._tempDir, "config.json");
        Assert.False(File.Exists(configPath), "Config file should not exist before test");

        // Act
        var config = ConfigParser.LoadFromFile(configPath);

        // Assert
        Assert.True(File.Exists(configPath), "Config file should be created");
        Assert.NotNull(config);
        Assert.NotEmpty(config.Nodes);
    }

    [Fact]
    public void LoadFromFile_WhenFileDoesNotExist_ConfigHasPathsRelativeToConfigFile()
    {
        // Arrange
        var configPath = Path.Combine(this._tempDir, "config.json");

        // Act
        var config = ConfigParser.LoadFromFile(configPath);

        // Assert - paths should be relative to config directory
        var personalNode = config.Nodes["personal"];

        // Content index should be SQLite
        var sqliteContent = Assert.IsType<SqliteContentIndexConfig>(personalNode.ContentIndex);
        Assert.Contains(this._tempDir, sqliteContent.Path);

        // Should follow structure: {tempDir}/nodes/personal/content.db
        var expectedContentPath = Path.Combine(this._tempDir, "nodes", "personal", "content.db");
        Assert.Equal(expectedContentPath, sqliteContent.Path);

        // FTS index path should also be under temp dir
        var ftsIndex = Assert.IsType<FtsSearchIndexConfig>(personalNode.SearchIndexes.First());
        Assert.Contains(this._tempDir, ftsIndex.Path!);

        var expectedFtsPath = Path.Combine(this._tempDir, "nodes", "personal", "fts.db");
        Assert.Equal(expectedFtsPath, ftsIndex.Path);
    }

    [Fact]
    public void LoadFromFile_WhenFileDoesNotExist_CreatedConfigIsValid()
    {
        // Arrange
        var configPath = Path.Combine(this._tempDir, "config.json");

        // Act
        var config = ConfigParser.LoadFromFile(configPath);

        // Assert - should not throw on validation
        config.Validate();
        
        // Verify structure
        Assert.Single(config.Nodes);
        Assert.True(config.Nodes.ContainsKey("personal"));
        Assert.NotNull(config.EmbeddingsCache);
        Assert.Null(config.LLMCache);
    }

    [Fact]
    public void LoadFromFile_WhenFileDoesNotExist_CreatedJsonIsValidAndParseable()
    {
        // Arrange
        var configPath = Path.Combine(this._tempDir, "config.json");

        // Act
        var config1 = ConfigParser.LoadFromFile(configPath);
        
        // Read it back and parse again
        var config2 = ConfigParser.LoadFromFile(configPath);

        // Assert - should be able to reload the created config
        Assert.NotNull(config2);
        Assert.Equal(config1.Nodes.Count, config2.Nodes.Count);
        Assert.Equal(config1.Nodes["personal"].Id, config2.Nodes["personal"].Id);
    }

    [Fact]
    public void LoadFromFile_WhenFileDoesNotExist_CreatedJsonHasNoNullFields()
    {
        // Arrange
        var configPath = Path.Combine(this._tempDir, "config.json");

        // Act
        ConfigParser.LoadFromFile(configPath);
        var json = File.ReadAllText(configPath);

        // Assert - no null fields should be serialized
        Assert.DoesNotContain("\"connectionString\": null", json);
        Assert.DoesNotContain("\"embeddings\": null", json);
        Assert.DoesNotContain("\"fileStorage\": null", json);
        Assert.DoesNotContain("\"repoStorage\": null", json);
        Assert.DoesNotContain("\"llmCache\": null", json);
    }

    [Fact]
    public void LoadFromFile_WhenDirectoryDoesNotExist_CreatesDirectory()
    {
        // Arrange
        var subDir = Path.Combine(this._tempDir, "nested", "deep");
        var configPath = Path.Combine(subDir, "config.json");
        Assert.False(Directory.Exists(subDir), "Directory should not exist before test");

        // Act
        var config = ConfigParser.LoadFromFile(configPath);

        // Assert
        Assert.True(Directory.Exists(subDir), "Directory should be created");
        Assert.True(File.Exists(configPath), "Config file should be created");
        Assert.NotNull(config);
    }
}
