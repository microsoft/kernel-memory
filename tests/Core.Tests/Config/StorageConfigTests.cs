using KernelMemory.Core.Config;
using KernelMemory.Core.Config.Validation;

namespace Core.Tests.Config;

/// <summary>
/// Tests for Storage configuration validation and parsing
/// </summary>
internal sealed class StorageConfigTests
{
    [Fact]
    public void LoadFromFile_WithDiskStorage_ShouldExpandTildePath()
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
                    ""fileStorage"": {
                        ""$type"": ""disk"",
                        ""path"": ""~/test-files""
                    },
                    ""repoStorage"": {
                        ""$type"": ""disk"",
                        ""path"": ""~/test-repos""
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
            var node = config.Nodes["test"];
            Assert.NotNull(node.FileStorage);
            Assert.NotNull(node.RepoStorage);

            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var fileStorage = Assert.IsType<KernelMemory.Core.Config.Storage.DiskStorageConfig>(node.FileStorage);
            var repoStorage = Assert.IsType<KernelMemory.Core.Config.Storage.DiskStorageConfig>(node.RepoStorage);
            Assert.StartsWith(homeDir, fileStorage.Path);
            Assert.StartsWith(homeDir, repoStorage.Path);
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
    public void LoadFromFile_WithDiskStorageMissingPath_ShouldThrowConfigException()
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
                    ""fileStorage"": {
                        ""$type"": ""disk"",
                        ""path"": """"
                    }
                }
            }
        }";

        try
        {
            File.WriteAllText(tempFile, json);

            // Act & Assert
            var exception = Assert.Throws<ConfigException>(() => ConfigParser.LoadFromFile(tempFile));
            Assert.Contains("Disk storage path is required", exception.Message);
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
    public void LoadFromFile_WithAzureBlobStorageConnectionString_ShouldValidate()
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
                    ""fileStorage"": {
                        ""$type"": ""azureBlobs"",
                        ""connectionString"": ""DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key;EndpointSuffix=core.windows.net""
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
            Assert.NotNull(config.Nodes["test"].FileStorage);
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
    public void LoadFromFile_WithAzureBlobStorageApiKey_ShouldValidate()
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
                    ""fileStorage"": {
                        ""$type"": ""azureBlobs"",
                        ""account"": ""testaccount"",
                        ""apiKey"": ""test-api-key""
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
            Assert.NotNull(config.Nodes["test"].FileStorage);
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
    public void LoadFromFile_WithAzureBlobStorageNoAuth_ShouldThrowConfigException()
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
                    ""fileStorage"": {
                        ""$type"": ""azureBlobs"",
                        ""account"": ""testaccount""
                    }
                }
            }
        }";

        try
        {
            File.WriteAllText(tempFile, json);

            // Act & Assert
            var exception = Assert.Throws<ConfigException>(() => ConfigParser.LoadFromFile(tempFile));
            Assert.Contains("Azure Blob storage requires one of", exception.Message);
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
    public void LoadFromFile_WithAzureBlobStorageMultipleAuth_ShouldThrowConfigException()
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
                    ""fileStorage"": {
                        ""$type"": ""azureBlobs"",
                        ""connectionString"": ""DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key"",
                        ""apiKey"": ""test-api-key""
                    }
                }
            }
        }";

        try
        {
            File.WriteAllText(tempFile, json);

            // Act & Assert
            var exception = Assert.Throws<ConfigException>(() => ConfigParser.LoadFromFile(tempFile));
            Assert.Contains("specify only one authentication method", exception.Message);
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
