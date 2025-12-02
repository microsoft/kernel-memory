// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Config;
using KernelMemory.Core.Config.Embeddings;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Config.Validation;

namespace KernelMemory.Core.Tests.Config;

/// <summary>
/// Tests for HuggingFaceEmbeddingsConfig to verify configuration parsing, validation, and defaults.
/// </summary>
public sealed class HuggingFaceEmbeddingsConfigTests
{
    [Fact]
    public void Type_ShouldReturnHuggingFace()
    {
        // Arrange
        var config = new HuggingFaceEmbeddingsConfig();

        // Assert
        Assert.Equal(EmbeddingsTypes.HuggingFace, config.Type);
    }

    [Fact]
    public void DefaultModel_ShouldBeAllMiniLM()
    {
        // Arrange
        var config = new HuggingFaceEmbeddingsConfig();

        // Assert
        Assert.Equal("sentence-transformers/all-MiniLM-L6-v2", config.Model);
    }

    [Fact]
    public void DefaultBaseUrl_ShouldBeInferenceApi()
    {
        // Arrange
        var config = new HuggingFaceEmbeddingsConfig();

        // Assert
        Assert.Equal("https://api-inference.huggingface.co", config.BaseUrl);
    }

    [Fact]
    public void ApiKey_ShouldDefaultToNull()
    {
        // Arrange
        var config = new HuggingFaceEmbeddingsConfig();

        // Assert
        Assert.Null(config.ApiKey);
    }

    [Fact]
    public void Validate_WithValidConfig_ShouldNotThrow()
    {
        // Arrange
        var config = new HuggingFaceEmbeddingsConfig
        {
            Model = "BAAI/bge-base-en-v1.5",
            ApiKey = "hf_test_token",
            BaseUrl = "https://api-inference.huggingface.co"
        };

        // Act & Assert - should not throw
        config.Validate("test");
    }

    [Fact]
    public void Validate_WithEmptyModel_ShouldThrowConfigException()
    {
        // Arrange
        var config = new HuggingFaceEmbeddingsConfig
        {
            Model = "",
            ApiKey = "hf_test_token"
        };

        // Act & Assert
        var ex = Assert.Throws<ConfigException>(() => config.Validate("test"));
        Assert.Contains("Model", ex.Message);
    }

    [Fact]
    public void Validate_WithEmptyApiKey_ShouldThrowConfigException()
    {
        // Arrange
        var config = new HuggingFaceEmbeddingsConfig
        {
            Model = "model",
            ApiKey = ""
        };

        // Act & Assert
        var ex = Assert.Throws<ConfigException>(() => config.Validate("test"));
        Assert.Contains("API", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WithNullApiKey_ShouldThrowConfigException()
    {
        // Arrange
        var config = new HuggingFaceEmbeddingsConfig
        {
            Model = "model",
            ApiKey = null
        };

        // Act & Assert
        var ex = Assert.Throws<ConfigException>(() => config.Validate("test"));
        Assert.Contains("API", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WithInvalidBaseUrl_ShouldThrowConfigException()
    {
        // Arrange
        var config = new HuggingFaceEmbeddingsConfig
        {
            Model = "model",
            ApiKey = "hf_token",
            BaseUrl = "not-a-valid-url"
        };

        // Act & Assert
        var ex = Assert.Throws<ConfigException>(() => config.Validate("test"));
        Assert.Contains("URL", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadFromFile_WithHuggingFaceEmbeddings_ShouldDeserialize()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"config-{Guid.NewGuid()}.json");
        const string json = @"{
            ""nodes"": {
                ""test"": {
                    ""id"": ""test"",
                    ""access"": ""Full"",
                    ""contentIndex"": {
                        ""type"": ""sqlite"",
                        ""path"": ""test.db""
                    },
                    ""searchIndexes"": [
                        {
                            ""type"": ""sqliteVector"",
                            ""id"": ""vector-test"",
                            ""path"": ""vector.db"",
                            ""dimensions"": 384,
                            ""embeddings"": {
                                ""type"": ""huggingFace"",
                                ""model"": ""sentence-transformers/all-MiniLM-L6-v2"",
                                ""apiKey"": ""hf_test_token"",
                                ""baseUrl"": ""https://api-inference.huggingface.co""
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
            var hfConfig = Assert.IsType<HuggingFaceEmbeddingsConfig>(searchIndex.Embeddings);
            Assert.Equal("sentence-transformers/all-MiniLM-L6-v2", hfConfig.Model);
            Assert.Equal("hf_test_token", hfConfig.ApiKey);
            Assert.Equal("https://api-inference.huggingface.co", hfConfig.BaseUrl);
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
