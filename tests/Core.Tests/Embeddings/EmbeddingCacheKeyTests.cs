// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Embeddings.Cache;

namespace KernelMemory.Core.Tests.Embeddings;

/// <summary>
/// Tests for EmbeddingCacheKey to verify correct cache key generation and SHA256 hashing.
/// Cache keys must uniquely identify embeddings by provider, model, dimensions, normalization, and text content.
/// </summary>
public sealed class EmbeddingCacheKeyTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldGenerateCorrectCacheKey()
    {
        // Arrange & Act
        var key = EmbeddingCacheKey.Create(
            provider: "OpenAI",
            model: "text-embedding-ada-002",
            vectorDimensions: 1536,
            isNormalized: true,
            text: "Hello world");

        // Assert
        Assert.Equal("OpenAI", key.Provider);
        Assert.Equal("text-embedding-ada-002", key.Model);
        Assert.Equal(1536, key.VectorDimensions);
        Assert.True(key.IsNormalized);
        Assert.Equal(11, key.TextLength);
        Assert.NotNull(key.TextHash);
        Assert.NotEmpty(key.TextHash);
    }

    [Fact]
    public void Create_WithSameText_ShouldGenerateSameHash()
    {
        // Arrange
        const string text = "Test embedding text";

        // Act
        var key1 = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, text);
        var key2 = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, text);

        // Assert
        Assert.Equal(key1.TextHash, key2.TextHash);
    }

    [Fact]
    public void Create_WithDifferentText_ShouldGenerateDifferentHash()
    {
        // Arrange
        const string text1 = "Text one";
        const string text2 = "Text two";

        // Act
        var key1 = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, text1);
        var key2 = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, text2);

        // Assert
        Assert.NotEqual(key1.TextHash, key2.TextHash);
    }

    [Fact]
    public void TextHash_ShouldBeSha256Format()
    {
        // Arrange & Act
        var key = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "test");

        // Assert - SHA256 produces 64 character hex string
        Assert.Equal(64, key.TextHash.Length);
        Assert.Matches("^[a-fA-F0-9]+$", key.TextHash);
    }

    [Fact]
    public void ToCompositeKey_ShouldIncludeAllComponents()
    {
        // Arrange
        var key = EmbeddingCacheKey.Create(
            provider: "Ollama",
            model: "qwen3-embedding",
            vectorDimensions: 1024,
            isNormalized: false,
            text: "sample");

        // Act
        var compositeKey = key.ToCompositeKey();

        // Assert
        Assert.Contains("Ollama", compositeKey);
        Assert.Contains("qwen3-embedding", compositeKey);
        Assert.Contains("1024", compositeKey);
        Assert.Contains("False", compositeKey);
        Assert.Contains("6", compositeKey); // Text length
        Assert.Contains(key.TextHash, compositeKey);
    }

    [Fact]
    public void ToCompositeKey_WithSameParameters_ShouldBeIdentical()
    {
        // Arrange
        var key1 = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "text");
        var key2 = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "text");

        // Act
        var composite1 = key1.ToCompositeKey();
        var composite2 = key2.ToCompositeKey();

        // Assert
        Assert.Equal(composite1, composite2);
    }

    [Fact]
    public void ToCompositeKey_WithDifferentProvider_ShouldBeDifferent()
    {
        // Arrange
        var key1 = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "text");
        var key2 = EmbeddingCacheKey.Create("Ollama", "model", 1536, true, "text");

        // Act & Assert
        Assert.NotEqual(key1.ToCompositeKey(), key2.ToCompositeKey());
    }

    [Fact]
    public void ToCompositeKey_WithDifferentModel_ShouldBeDifferent()
    {
        // Arrange
        var key1 = EmbeddingCacheKey.Create("OpenAI", "model-a", 1536, true, "text");
        var key2 = EmbeddingCacheKey.Create("OpenAI", "model-b", 1536, true, "text");

        // Act & Assert
        Assert.NotEqual(key1.ToCompositeKey(), key2.ToCompositeKey());
    }

    [Fact]
    public void ToCompositeKey_WithDifferentDimensions_ShouldBeDifferent()
    {
        // Arrange
        var key1 = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "text");
        var key2 = EmbeddingCacheKey.Create("OpenAI", "model", 768, true, "text");

        // Act & Assert
        Assert.NotEqual(key1.ToCompositeKey(), key2.ToCompositeKey());
    }

    [Fact]
    public void ToCompositeKey_WithDifferentNormalization_ShouldBeDifferent()
    {
        // Arrange
        var key1 = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "text");
        var key2 = EmbeddingCacheKey.Create("OpenAI", "model", 1536, false, "text");

        // Act & Assert
        Assert.NotEqual(key1.ToCompositeKey(), key2.ToCompositeKey());
    }

    [Fact]
    public void Create_WithEmptyText_ShouldGenerateValidHash()
    {
        // Arrange & Act
        var key = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "");

        // Assert
        Assert.Equal(0, key.TextLength);
        Assert.NotNull(key.TextHash);
        Assert.Equal(64, key.TextHash.Length);
    }

    [Fact]
    public void Create_WithUnicodeText_ShouldGenerateValidHash()
    {
        // Arrange & Act
        var key = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, "Hello, ");

        // Assert
        Assert.NotNull(key.TextHash);
        Assert.Equal(64, key.TextHash.Length);
    }

    [Fact]
    public void Create_WithLongText_ShouldCaptureCorrectLength()
    {
        // Arrange
        var longText = new string('a', 10000);

        // Act
        var key = EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, longText);

        // Assert
        Assert.Equal(10000, key.TextLength);
    }

    [Fact]
    public void Create_WithNullText_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            EmbeddingCacheKey.Create("OpenAI", "model", 1536, true, null!));
    }

    [Fact]
    public void Create_WithNullProvider_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            EmbeddingCacheKey.Create(null!, "model", 1536, true, "text"));
    }

    [Fact]
    public void Create_WithNullModel_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            EmbeddingCacheKey.Create("OpenAI", null!, 1536, true, "text"));
    }

    [Fact]
    public void Create_WithZeroDimensions_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EmbeddingCacheKey.Create("OpenAI", "model", 0, true, "text"));
    }

    [Fact]
    public void Create_WithNegativeDimensions_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EmbeddingCacheKey.Create("OpenAI", "model", -1, true, "text"));
    }
}
