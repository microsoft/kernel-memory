// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Config.Embeddings;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Embeddings;
using KernelMemory.Core.Embeddings.Cache;
using KernelMemory.Core.Embeddings.Providers;
using KernelMemory.Main.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace KernelMemory.Main.Tests.Services;

/// <summary>
/// Unit tests for EmbeddingGeneratorFactory.
/// Tests verify correct generator creation from configuration.
/// </summary>
public sealed class EmbeddingGeneratorFactoryTests : IDisposable
{
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly HttpClient _httpClient;

    public EmbeddingGeneratorFactoryTests()
    {
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
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void CreateGenerator_CreatesOllamaGenerator()
    {
        // Arrange
        var config = new OllamaEmbeddingsConfig
        {
            Model = "qwen3-embedding",
            BaseUrl = "http://localhost:11434"
        };

        // Act
        var generator = EmbeddingGeneratorFactory.CreateGenerator(
            config,
            this._httpClient,
            cache: null,
            this._mockLoggerFactory.Object);

        // Assert
        Assert.NotNull(generator);
        Assert.Equal(EmbeddingsTypes.Ollama, generator.ProviderType);
        Assert.Equal("qwen3-embedding", generator.ModelName);
        Assert.Equal(1024, generator.VectorDimensions); // Known dimension for qwen3-embedding
    }

    [Fact]
    public void CreateGenerator_CreatesOpenAIGenerator()
    {
        // Arrange
        var config = new OpenAIEmbeddingsConfig
        {
            Model = "text-embedding-3-small",
            ApiKey = "test-api-key"
        };

        // Act
        var generator = EmbeddingGeneratorFactory.CreateGenerator(
            config,
            this._httpClient,
            cache: null,
            this._mockLoggerFactory.Object);

        // Assert
        Assert.NotNull(generator);
        Assert.Equal(EmbeddingsTypes.OpenAI, generator.ProviderType);
        Assert.Equal("text-embedding-3-small", generator.ModelName);
        Assert.Equal(1536, generator.VectorDimensions); // Known dimension for text-embedding-3-small
    }

    [Fact]
    public void CreateGenerator_CreatesAzureOpenAIGenerator()
    {
        // Arrange
        var config = new AzureOpenAIEmbeddingsConfig
        {
            Model = "text-embedding-ada-002",
            Endpoint = "https://test.openai.azure.com",
            Deployment = "test-deployment",
            ApiKey = "test-api-key"
        };

        // Act
        var generator = EmbeddingGeneratorFactory.CreateGenerator(
            config,
            this._httpClient,
            cache: null,
            this._mockLoggerFactory.Object);

        // Assert
        Assert.NotNull(generator);
        Assert.Equal(EmbeddingsTypes.AzureOpenAI, generator.ProviderType);
        Assert.Equal("text-embedding-ada-002", generator.ModelName);
        Assert.Equal(1536, generator.VectorDimensions); // Known dimension for ada-002
    }

    [Fact]
    public void CreateGenerator_CreatesHuggingFaceGenerator()
    {
        // Arrange
        var config = new HuggingFaceEmbeddingsConfig
        {
            Model = "sentence-transformers/all-MiniLM-L6-v2",
            ApiKey = "test-api-key"
        };

        // Act
        var generator = EmbeddingGeneratorFactory.CreateGenerator(
            config,
            this._httpClient,
            cache: null,
            this._mockLoggerFactory.Object);

        // Assert
        Assert.NotNull(generator);
        Assert.Equal(EmbeddingsTypes.HuggingFace, generator.ProviderType);
        Assert.Equal("sentence-transformers/all-MiniLM-L6-v2", generator.ModelName);
        Assert.Equal(384, generator.VectorDimensions); // Known dimension for all-MiniLM-L6-v2
    }

    [Fact]
    public void CreateGenerator_CreatesHuggingFaceGenerator_FromHfTokenEnvVar()
    {
        // Arrange
        var originalToken = Environment.GetEnvironmentVariable("HF_TOKEN");
        Environment.SetEnvironmentVariable("HF_TOKEN", "hf_test_token_from_env");

        try
        {
            var config = new HuggingFaceEmbeddingsConfig
            {
                Model = "sentence-transformers/all-MiniLM-L6-v2",
                ApiKey = null
            };

            config.Validate("Embeddings");

            // Act
            var generator = EmbeddingGeneratorFactory.CreateGenerator(
                config,
                this._httpClient,
                cache: null,
                this._mockLoggerFactory.Object);

            // Assert
            Assert.NotNull(generator);
            Assert.Equal(EmbeddingsTypes.HuggingFace, generator.ProviderType);
        }
        finally
        {
            Environment.SetEnvironmentVariable("HF_TOKEN", originalToken);
        }
    }

    [Fact]
    public void CreateGenerator_WrapsWithCacheWhenProvided()
    {
        // Arrange
        var config = new OllamaEmbeddingsConfig
        {
            Model = "qwen3-embedding",
            BaseUrl = "http://localhost:11434"
        };

        var mockCache = new Mock<IEmbeddingCache>();
        mockCache.Setup(x => x.Mode).Returns(CacheModes.ReadWrite);

        // Act
        var generator = EmbeddingGeneratorFactory.CreateGenerator(
            config,
            this._httpClient,
            mockCache.Object,
            this._mockLoggerFactory.Object);

        // Assert
        Assert.NotNull(generator);
        Assert.IsType<CachedEmbeddingGenerator>(generator);
    }

    [Fact]
    public void CreateGenerator_DoesNotWrapWithCacheWhenNull()
    {
        // Arrange
        var config = new OllamaEmbeddingsConfig
        {
            Model = "qwen3-embedding",
            BaseUrl = "http://localhost:11434"
        };

        // Act
        var generator = EmbeddingGeneratorFactory.CreateGenerator(
            config,
            this._httpClient,
            cache: null,
            this._mockLoggerFactory.Object);

        // Assert
        Assert.NotNull(generator);
        Assert.IsType<OllamaEmbeddingGenerator>(generator);
    }

    [Fact]
    public void CreateGenerator_ThrowsForNullConfig()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            EmbeddingGeneratorFactory.CreateGenerator(
                null!,
                this._httpClient,
                cache: null,
                this._mockLoggerFactory.Object));
    }

    [Fact]
    public void CreateGenerator_ThrowsForNullHttpClient()
    {
        // Arrange
        var config = new OllamaEmbeddingsConfig
        {
            Model = "qwen3-embedding",
            BaseUrl = "http://localhost:11434"
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            EmbeddingGeneratorFactory.CreateGenerator(
                config,
                null!,
                cache: null,
                this._mockLoggerFactory.Object));
    }

    [Fact]
    public void CreateGenerator_ThrowsForNullLoggerFactory()
    {
        // Arrange
        var config = new OllamaEmbeddingsConfig
        {
            Model = "qwen3-embedding",
            BaseUrl = "http://localhost:11434"
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            EmbeddingGeneratorFactory.CreateGenerator(
                config,
                this._httpClient,
                cache: null,
                null!));
    }

    [Fact]
    public void CreateGenerator_UsesDefaultDimensionsForUnknownModel()
    {
        // Arrange
        var config = new OllamaEmbeddingsConfig
        {
            Model = "unknown-model-xyz",
            BaseUrl = "http://localhost:11434"
        };

        // Act
        var generator = EmbeddingGeneratorFactory.CreateGenerator(
            config,
            this._httpClient,
            cache: null,
            this._mockLoggerFactory.Object);

        // Assert - Should use default Ollama dimension
        Assert.NotNull(generator);
        Assert.True(generator.VectorDimensions > 0);
    }

    [Fact]
    public void CreateGenerator_SetsIsNormalizedTrue()
    {
        // Arrange
        var ollamaConfig = new OllamaEmbeddingsConfig
        {
            Model = "qwen3-embedding",
            BaseUrl = "http://localhost:11434"
        };

        var openaiConfig = new OpenAIEmbeddingsConfig
        {
            Model = "text-embedding-3-small",
            ApiKey = "test-key"
        };

        // Act
        var ollamaGen = EmbeddingGeneratorFactory.CreateGenerator(
            ollamaConfig,
            this._httpClient,
            cache: null,
            this._mockLoggerFactory.Object);

        var openaiGen = EmbeddingGeneratorFactory.CreateGenerator(
            openaiConfig,
            this._httpClient,
            cache: null,
            this._mockLoggerFactory.Object);

        // Assert - All generators should be normalized
        Assert.True(ollamaGen.IsNormalized);
        Assert.True(openaiGen.IsNormalized);
    }
}
