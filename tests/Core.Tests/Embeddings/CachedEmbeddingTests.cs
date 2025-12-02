// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Embeddings.Cache;

namespace KernelMemory.Core.Tests.Embeddings;

/// <summary>
/// Tests for CachedEmbedding model to verify proper construction and immutability.
/// CachedEmbedding stores the embedding vector, optional token count, and timestamp.
/// </summary>
public sealed class CachedEmbeddingTests
{
    [Fact]
    public void CachedEmbedding_WithRequiredProperties_ShouldBeCreated()
    {
        // Arrange
        var vector = new float[] { 0.1f, 0.2f, 0.3f };
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var cached = new CachedEmbedding
        {
            Vector = vector,
            Timestamp = timestamp
        };

        // Assert
        Assert.Equal(vector, cached.Vector);
        Assert.Equal(timestamp, cached.Timestamp);
        Assert.Null(cached.TokenCount);
    }

    [Fact]
    public void CachedEmbedding_WithTokenCount_ShouldStoreValue()
    {
        // Arrange
        var vector = new float[] { 0.1f, 0.2f, 0.3f };
        var timestamp = DateTimeOffset.UtcNow;
        const int tokenCount = 42;

        // Act
        var cached = new CachedEmbedding
        {
            Vector = vector,
            Timestamp = timestamp,
            TokenCount = tokenCount
        };

        // Assert
        Assert.Equal(tokenCount, cached.TokenCount);
    }

    [Fact]
    public void CachedEmbedding_WithNullTokenCount_ShouldBeNull()
    {
        // Arrange
        var vector = new float[] { 0.1f, 0.2f, 0.3f };
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var cached = new CachedEmbedding
        {
            Vector = vector,
            Timestamp = timestamp,
            TokenCount = null
        };

        // Assert
        Assert.Null(cached.TokenCount);
    }

    [Fact]
    public void CachedEmbedding_VectorShouldPreserveFloatPrecision()
    {
        // Arrange
        var vector = new float[] { 0.123456789f, -0.987654321f, float.MaxValue, float.MinValue };
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var cached = new CachedEmbedding
        {
            Vector = vector,
            Timestamp = timestamp
        };

        // Assert
        Assert.Equal(0.123456789f, cached.Vector[0]);
        Assert.Equal(-0.987654321f, cached.Vector[1]);
        Assert.Equal(float.MaxValue, cached.Vector[2]);
        Assert.Equal(float.MinValue, cached.Vector[3]);
    }

    [Fact]
    public void CachedEmbedding_WithLargeVector_ShouldPreserveAllDimensions()
    {
        // Arrange - simulate 1536-dimension embedding (OpenAI ada-002)
        var vector = new float[1536];
        for (int i = 0; i < vector.Length; i++)
        {
            vector[i] = (float)i / 1536;
        }

        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var cached = new CachedEmbedding
        {
            Vector = vector,
            Timestamp = timestamp
        };

        // Assert
        Assert.Equal(1536, cached.Vector.Length);
        Assert.Equal(0.0f, cached.Vector[0]);
        Assert.Equal(1535f / 1536, cached.Vector[1535], precision: 6);
    }

    [Fact]
    public void CachedEmbedding_TimestampShouldBeDateTimeOffset()
    {
        // Arrange
        var vector = new float[] { 0.1f };
        var timestamp = new DateTimeOffset(2025, 12, 1, 10, 30, 0, TimeSpan.FromHours(-5));

        // Act
        var cached = new CachedEmbedding
        {
            Vector = vector,
            Timestamp = timestamp
        };

        // Assert
        Assert.Equal(timestamp.Year, cached.Timestamp.Year);
        Assert.Equal(timestamp.Month, cached.Timestamp.Month);
        Assert.Equal(timestamp.Day, cached.Timestamp.Day);
        Assert.Equal(timestamp.Hour, cached.Timestamp.Hour);
        Assert.Equal(timestamp.Offset, cached.Timestamp.Offset);
    }
}
