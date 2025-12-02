// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Embeddings;

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
        var exists = EmbeddingConstants.KnownModelDimensions.TryGetValue(modelName, out var dimensions);

        // Assert
        Assert.True(exists, $"Model '{modelName}' should be in KnownModelDimensions");
        Assert.Equal(expectedDimensions, dimensions);
    }

    [Fact]
    public void KnownModelDimensions_ShouldNotBeEmpty()
    {
        // Assert
        Assert.NotEmpty(EmbeddingConstants.KnownModelDimensions);
    }

    [Fact]
    public void KnownModelDimensions_AllValuesShouldBePositive()
    {
        // Assert
        foreach (var kvp in EmbeddingConstants.KnownModelDimensions)
        {
            Assert.True(kvp.Value > 0, $"Model '{kvp.Key}' has invalid dimensions: {kvp.Value}");
        }
    }

    [Fact]
    public void TryGetDimensions_WithKnownModel_ShouldReturnTrue()
    {
        // Act
        var result = EmbeddingConstants.TryGetDimensions("text-embedding-ada-002", out var dimensions);

        // Assert
        Assert.True(result);
        Assert.Equal(1536, dimensions);
    }

    [Fact]
    public void TryGetDimensions_WithUnknownModel_ShouldReturnFalse()
    {
        // Act
        var result = EmbeddingConstants.TryGetDimensions("unknown-model", out var dimensions);

        // Assert
        Assert.False(result);
        Assert.Equal(0, dimensions);
    }

    [Fact]
    public void DefaultBatchSize_ShouldBe10()
    {
        // Assert
        Assert.Equal(10, EmbeddingConstants.DefaultBatchSize);
    }

    [Fact]
    public void DefaultOllamaModel_ShouldBeQwen3Embedding()
    {
        // Assert
        Assert.Equal("qwen3-embedding", EmbeddingConstants.DefaultOllamaModel);
    }

    [Fact]
    public void DefaultOllamaBaseUrl_ShouldBeLocalhost()
    {
        // Assert
        Assert.Equal("http://localhost:11434", EmbeddingConstants.DefaultOllamaBaseUrl);
    }

    [Fact]
    public void DefaultHuggingFaceModel_ShouldBeAllMiniLM()
    {
        // Assert
        Assert.Equal("sentence-transformers/all-MiniLM-L6-v2", EmbeddingConstants.DefaultHuggingFaceModel);
    }

    [Fact]
    public void DefaultHuggingFaceBaseUrl_ShouldBeInferenceApi()
    {
        // Assert
        Assert.Equal("https://api-inference.huggingface.co", EmbeddingConstants.DefaultHuggingFaceBaseUrl);
    }
}
