// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Tests.Embeddings;

/// <summary>
/// Tests for EmbeddingConstants to verify known model dimensions are correct.
/// These values are critical for cache key generation and validation.
/// </summary>
public sealed class EmbeddingConstantsTests
{
    [Theory]
    [InlineData("qwen3-embedding", 1024)]
    [InlineData("nomic-embed-text", 768)]
    [InlineData("text-embedding-ada-002", 1536)]
    [InlineData("text-embedding-3-small", 1536)]
    [InlineData("text-embedding-3-large", 3072)]
    [InlineData("sentence-transformers/all-MiniLM-L6-v2", 384)]
    [InlineData("BAAI/bge-base-en-v1.5", 768)]
    public void KnownModelDimensions_ShouldContainExpectedValues(string modelName, int expectedDimensions)
    {
        // Act
        var exists = Constants.EmbeddingDefaults.KnownModelDimensions.TryGetValue(modelName, out var dimensions);

        // Assert
        Assert.True(exists, $"Model '{modelName}' should be in KnownModelDimensions");
        Assert.Equal(expectedDimensions, dimensions);
    }

    [Fact]
    public void KnownModelDimensions_ShouldNotBeEmpty()
    {
        // Assert
        Assert.NotEmpty(Constants.EmbeddingDefaults.KnownModelDimensions);
    }

    [Fact]
    public void KnownModelDimensions_AllValuesShouldBePositive()
    {
        // Assert
        foreach (var kvp in Constants.EmbeddingDefaults.KnownModelDimensions)
        {
            Assert.True(kvp.Value > 0, $"Model '{kvp.Key}' has invalid dimensions: {kvp.Value}");
        }
    }

    [Fact]
    public void TryGetDimensions_WithKnownModel_ShouldReturnTrue()
    {
        // Act
        var result = Constants.EmbeddingDefaults.TryGetDimensions("text-embedding-ada-002", out var dimensions);

        // Assert
        Assert.True(result);
        Assert.Equal(1536, dimensions);
    }

    [Fact]
    public void TryGetDimensions_WithUnknownModel_ShouldReturnFalse()
    {
        // Act
        var result = Constants.EmbeddingDefaults.TryGetDimensions("unknown-model", out var dimensions);

        // Assert
        Assert.False(result);
        Assert.Equal(0, dimensions);
    }

    [Fact]
    public void DefaultBatchSize_ShouldBe10()
    {
        // Assert
        Assert.Equal(10, Constants.EmbeddingDefaults.DefaultBatchSize);
    }

    [Fact]
    public void DefaultOllamaModel_ShouldBeQwen3Embedding()
    {
        // Assert
        Assert.Equal("qwen3-embedding:0.6b", Constants.EmbeddingDefaults.DefaultOllamaModel);
    }

    [Fact]
    public void DefaultOllamaBaseUrl_ShouldBeLocalhost()
    {
        // Assert
        Assert.Equal("http://localhost:11434", Constants.EmbeddingDefaults.DefaultOllamaBaseUrl);
    }

    [Fact]
    public void DefaultHuggingFaceModel_ShouldBeAllMiniLM()
    {
        // Assert
        Assert.Equal("sentence-transformers/all-MiniLM-L6-v2", Constants.EmbeddingDefaults.DefaultHuggingFaceModel);
    }

    [Fact]
    public void DefaultHuggingFaceBaseUrl_ShouldBeInferenceApi()
    {
        // Assert
        Assert.Equal("https://api-inference.huggingface.co", Constants.EmbeddingDefaults.DefaultHuggingFaceBaseUrl);
    }
}
