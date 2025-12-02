// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Embeddings.Cache;

namespace KernelMemory.Core.Tests.Embeddings;

/// <summary>
/// Tests for CachedEmbedding model to verify proper construction.
/// CachedEmbedding stores the embedding vector.
/// </summary>
public sealed class CachedEmbeddingTests
{
    [Fact]
    public void CachedEmbedding_WithRequiredProperties_ShouldBeCreated()
    {
        // Arrange
        var vector = new float[] { 0.1f, 0.2f, 0.3f };

        // Act
        var cached = new CachedEmbedding
        {
            Vector = vector
        };

        // Assert
        Assert.Equal(vector, cached.Vector);
    }

    [Fact]
    public void CachedEmbedding_VectorShouldPreserveFloatPrecision()
    {
        // Arrange
        var vector = new float[] { 0.123456789f, -0.987654321f, float.MaxValue, float.MinValue };

        // Act
        var cached = new CachedEmbedding
        {
            Vector = vector
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

        // Act
        var cached = new CachedEmbedding
        {
            Vector = vector
        };

        // Assert
        Assert.Equal(1536, cached.Vector.Length);
        Assert.Equal(0.0f, cached.Vector[0]);
        Assert.Equal(1535f / 1536, cached.Vector[1535], precision: 6);
    }
}
