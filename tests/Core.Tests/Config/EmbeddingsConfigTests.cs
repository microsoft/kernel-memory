using KernelMemory.Core.Config;
using KernelMemory.Core.Config.Validation;

namespace Core.Tests.Config;

/// <summary>
/// Tests for Embeddings configuration validation
/// </summary>
internal sealed class EmbeddingsConfigTests
{
    [Fact]
    public void LoadFromFile_WithOllamaEmbeddings_ShouldValidate()
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
                            ""$type"": ""vector"",
                            ""type"": ""SqliteVector"",
                            ""path"": ""vector.db"",
                            ""dimensions"": 384,
                            ""embeddings"": {
                                ""$type"": ""ollama"",
                                ""model"": ""all-minilm"",
                                ""baseUrl"": ""http://localhost:11434""
                            }
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
            var searchIndex = config.Nodes["test"].SearchIndexes[0];
            Assert.NotNull(searchIndex.Embeddings);
            var ollamaConfig = Assert.IsType<KernelMemory.Core.Config.Embeddings.OllamaEmbeddingsConfig>(searchIndex.Embeddings);
            Assert.Equal("all-minilm", ollamaConfig.Model);
            Assert.Equal("http://localhost:11434", ollamaConfig.BaseUrl);
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
    public void LoadFromFile_WithOpenAIEmbeddings_ShouldValidate()
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
                            ""$type"": ""vector"",
                            ""type"": ""SqliteVector"",
                            ""path"": ""vector.db"",
                            ""dimensions"": 1536,
                            ""embeddings"": {
                                ""$type"": ""openai"",
                                ""model"": ""text-embedding-ada-002"",
                                ""apiKey"": ""test-key"",
                                ""baseUrl"": ""https://api.openai.com/v1""
                            }
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
            var searchIndex = config.Nodes["test"].SearchIndexes[0];
            Assert.NotNull(searchIndex.Embeddings);
            var openaiConfig = Assert.IsType<KernelMemory.Core.Config.Embeddings.OpenAIEmbeddingsConfig>(searchIndex.Embeddings);
            Assert.Equal("text-embedding-ada-002", openaiConfig.Model);
            Assert.Equal("test-key", openaiConfig.ApiKey);
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
    public void LoadFromFile_WithOllamaEmbeddingsMissingModel_ShouldThrowConfigException()
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
                            ""$type"": ""vector"",
                            ""type"": ""SqliteVector"",
                            ""path"": ""vector.db"",
                            ""dimensions"": 384,
                            ""embeddings"": {
                                ""$type"": ""ollama"",
                                ""model"": """",
                                ""baseUrl"": ""http://localhost:11434""
                            }
                        }
                    ]
                }
            }
        }";

        try
        {
            File.WriteAllText(tempFile, json);

            // Act & Assert
            var exception = Assert.Throws<ConfigException>(() => ConfigParser.LoadFromFile(tempFile));
            Assert.Contains("Ollama model name is required", exception.Message);
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
    public void LoadFromFile_WithOllamaEmbeddingsMissingBaseUrl_ShouldThrowConfigException()
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
                            ""$type"": ""vector"",
                            ""type"": ""SqliteVector"",
                            ""path"": ""vector.db"",
                            ""dimensions"": 384,
                            ""embeddings"": {
                                ""$type"": ""ollama"",
                                ""model"": ""all-minilm"",
                                ""baseUrl"": """"
                            }
                        }
                    ]
                }
            }
        }";

        try
        {
            File.WriteAllText(tempFile, json);

            // Act & Assert
            var exception = Assert.Throws<ConfigException>(() => ConfigParser.LoadFromFile(tempFile));
            Assert.Contains("Ollama base URL is required", exception.Message);
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
    public void LoadFromFile_WithOpenAIEmbeddingsMissingApiKey_ShouldThrowConfigException()
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
                            ""$type"": ""vector"",
                            ""type"": ""SqliteVector"",
                            ""path"": ""vector.db"",
                            ""dimensions"": 1536,
                            ""embeddings"": {
                                ""$type"": ""openai"",
                                ""model"": ""text-embedding-ada-002"",
                                ""apiKey"": """"
                            }
                        }
                    ]
                }
            }
        }";

        try
        {
            File.WriteAllText(tempFile, json);

            // Act & Assert
            var exception = Assert.Throws<ConfigException>(() => ConfigParser.LoadFromFile(tempFile));
            Assert.Contains("OpenAI API key is required", exception.Message);
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
    public void LoadFromFile_WithAzureOpenAIEmbeddingsMissingModel_ShouldThrowConfigException()
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
                            ""$type"": ""vector"",
                            ""type"": ""SqliteVector"",
                            ""path"": ""vector.db"",
                            ""dimensions"": 1536,
                            ""embeddings"": {
                                ""$type"": ""azureOpenAI"",
                                ""model"": """",
                                ""endpoint"": ""https://test.openai.azure.com"",
                                ""deployment"": ""test-deployment"",
                                ""apiKey"": ""test-key""
                            }
                        }
                    ]
                }
            }
        }";

        try
        {
            File.WriteAllText(tempFile, json);

            // Act & Assert
            var exception = Assert.Throws<ConfigException>(() => ConfigParser.LoadFromFile(tempFile));
            Assert.Contains("Model name is required", exception.Message);
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
    public void LoadFromFile_WithAzureOpenAIEmbeddingsMissingEndpoint_ShouldThrowConfigException()
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
                            ""$type"": ""vector"",
                            ""type"": ""SqliteVector"",
                            ""path"": ""vector.db"",
                            ""dimensions"": 1536,
                            ""embeddings"": {
                                ""$type"": ""azureOpenAI"",
                                ""model"": ""text-embedding-ada-002"",
                                ""endpoint"": """",
                                ""deployment"": ""test-deployment"",
                                ""apiKey"": ""test-key""
                            }
                        }
                    ]
                }
            }
        }";

        try
        {
            File.WriteAllText(tempFile, json);

            // Act & Assert
            var exception = Assert.Throws<ConfigException>(() => ConfigParser.LoadFromFile(tempFile));
            Assert.Contains("Azure OpenAI endpoint is required", exception.Message);
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
    public void LoadFromFile_WithAzureOpenAIEmbeddingsMissingDeployment_ShouldThrowConfigException()
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
                            ""$type"": ""vector"",
                            ""type"": ""SqliteVector"",
                            ""path"": ""vector.db"",
                            ""dimensions"": 1536,
                            ""embeddings"": {
                                ""$type"": ""azureOpenAI"",
                                ""model"": ""text-embedding-ada-002"",
                                ""endpoint"": ""https://test.openai.azure.com"",
                                ""deployment"": """",
                                ""apiKey"": ""test-key""
                            }
                        }
                    ]
                }
            }
        }";

        try
        {
            File.WriteAllText(tempFile, json);

            // Act & Assert
            var exception = Assert.Throws<ConfigException>(() => ConfigParser.LoadFromFile(tempFile));
            Assert.Contains("Deployment name is required", exception.Message);
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
    public void LoadFromFile_WithAzureOpenAIEmbeddingsNoAuth_ShouldThrowConfigException()
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
                            ""$type"": ""vector"",
                            ""type"": ""SqliteVector"",
                            ""path"": ""vector.db"",
                            ""dimensions"": 1536,
                            ""embeddings"": {
                                ""$type"": ""azureOpenAI"",
                                ""model"": ""text-embedding-ada-002"",
                                ""endpoint"": ""https://test.openai.azure.com"",
                                ""deployment"": ""test-deployment""
                            }
                        }
                    ]
                }
            }
        }";

        try
        {
            File.WriteAllText(tempFile, json);

            // Act & Assert
            var exception = Assert.Throws<ConfigException>(() => ConfigParser.LoadFromFile(tempFile));
            Assert.Contains("requires either ApiKey or UseManagedIdentity", exception.Message);
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
    public void LoadFromFile_WithAzureOpenAIEmbeddings_ShouldValidate()
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
                            ""$type"": ""vector"",
                            ""type"": ""SqliteVector"",
                            ""path"": ""vector.db"",
                            ""dimensions"": 1536,
                            ""embeddings"": {
                                ""$type"": ""azureOpenAI"",
                                ""model"": ""text-embedding-ada-002"",
                                ""endpoint"": ""https://test.openai.azure.com"",
                                ""deployment"": ""text-embedding-ada-002"",
                                ""apiKey"": ""test-key""
                            }
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
            var searchIndex = config.Nodes["test"].SearchIndexes[0];
            Assert.NotNull(searchIndex.Embeddings);
            var azureConfig = Assert.IsType<KernelMemory.Core.Config.Embeddings.AzureOpenAIEmbeddingsConfig>(searchIndex.Embeddings);
            Assert.Equal("text-embedding-ada-002", azureConfig.Model);
            Assert.Equal("https://test.openai.azure.com", azureConfig.Endpoint);
            Assert.Equal("text-embedding-ada-002", azureConfig.Deployment);
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
