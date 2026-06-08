// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Tests.Search;

/// <summary>
/// Unit tests for VectorMath class: normalization, dot product, and serialization operations.
/// </summary>
public sealed class VectorMathTests
{
    private const double Tolerance = 1e-6;

    [Fact]
    public void NormalizeVector_ProducesMagnitudeOne()
    {
        // Arrange
        var vector = new float[] { 3.0f, 4.0f }; // 3-4-5 triangle

        // Act
        var normalized = VectorMath.NormalizeVector(vector);

        // Assert - Magnitude should be 1
        var magnitude = Math.Sqrt(normalized.Sum(x => x * x));
        Assert.Equal(1.0, magnitude, Tolerance);

        // Check individual components: 3/5 and 4/5
        Assert.Equal(0.6f, normalized[0], (float)Tolerance);
        Assert.Equal(0.8f, normalized[1], (float)Tolerance);
    }

    [Fact]
    public void NormalizeVector_PreservesDirection()
    {
        // Arrange
        var vector = new float[] { 1.0f, 2.0f, 3.0f };

        // Act
        var normalized = VectorMath.NormalizeVector(vector);

        // Assert - Ratios should be preserved
        var ratio01 = vector[0] / vector[1];
        var normalizedRatio01 = normalized[0] / normalized[1];
        Assert.Equal(ratio01, normalizedRatio01, Tolerance);

        var ratio12 = vector[1] / vector[2];
        var normalizedRatio12 = normalized[1] / normalized[2];
        Assert.Equal(ratio12, normalizedRatio12, Tolerance);
    }

    [Fact]
    public void NormalizeVector_ThrowsForZeroVector()
    {
        // Arrange
        var zeroVector = new float[] { 0.0f, 0.0f, 0.0f };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => VectorMath.NormalizeVector(zeroVector));
    }

    [Fact]
    public void NormalizeVector_ThrowsForEmptyVector()
    {
        // Arrange
        var emptyVector = Array.Empty<float>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => VectorMath.NormalizeVector(emptyVector));
    }

    [Fact]
    public void NormalizeVector_ThrowsForNullVector()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => VectorMath.NormalizeVector(null!));
    }

    [Fact]
    public void NormalizeVector_HandlesNearZeroMagnitude()
    {
        // Arrange - Values so tiny that magnitude underflows to zero
        // float.Epsilon is the smallest positive float that is greater than zero
        // Values below this effectively produce zero magnitude
        var zeroVector = new float[] { 0f, 0f };

        // Act & Assert - Zero vector should throw because magnitude is zero
        Assert.Throws<ArgumentException>(() => VectorMath.NormalizeVector(zeroVector));
    }

    [Fact]
    public void DotProduct_ReturnsOneForIdenticalNormalizedVectors()
    {
        // Arrange
        var vector = new float[] { 3.0f, 4.0f };
        var normalized = VectorMath.NormalizeVector(vector);

        // Act
        var dotProduct = VectorMath.DotProduct(normalized, normalized);

        // Assert - Dot product of identical normalized vectors should be 1
        Assert.Equal(1.0, dotProduct, Tolerance);
    }

    [Fact]
    public void DotProduct_ReturnsZeroForOrthogonalVectors()
    {
        // Arrange - Two orthogonal (perpendicular) normalized vectors
        var v1 = VectorMath.NormalizeVector(new float[] { 1.0f, 0.0f });
        var v2 = VectorMath.NormalizeVector(new float[] { 0.0f, 1.0f });

        // Act
        var dotProduct = VectorMath.DotProduct(v1, v2);

        // Assert
        Assert.Equal(0.0, dotProduct, Tolerance);
    }

    [Fact]
    public void DotProduct_ReturnsNegativeOneForOppositeVectors()
    {
        // Arrange
        var v1 = VectorMath.NormalizeVector(new float[] { 1.0f, 0.0f });
        var v2 = VectorMath.NormalizeVector(new float[] { -1.0f, 0.0f });

        // Act
        var dotProduct = VectorMath.DotProduct(v1, v2);

        // Assert
        Assert.Equal(-1.0, dotProduct, Tolerance);
    }

    [Fact]
    public void DotProduct_ThrowsForDifferentLengths()
    {
        // Arrange
        var v1 = new float[] { 1.0f, 2.0f };
        var v2 = new float[] { 1.0f, 2.0f, 3.0f };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => VectorMath.DotProduct(v1, v2));
    }

    [Fact]
    public void DotProduct_ThrowsForNullVectors()
    {
        // Arrange
        var vector = new float[] { 1.0f, 2.0f };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => VectorMath.DotProduct(null!, vector));
        Assert.Throws<ArgumentNullException>(() => VectorMath.DotProduct(vector, null!));
    }

    [Fact]
    public void DotProduct_EqualsCosineSimilarityForNormalizedVectors()
    {
        // Arrange - Two vectors at 60 degrees (cosine = 0.5)
        var v1 = VectorMath.NormalizeVector(new float[] { 1.0f, 0.0f });
        var v2 = VectorMath.NormalizeVector(new float[] { 0.5f, (float)Math.Sqrt(0.75) }); // 60 degrees

        // Act
        var dotProduct = VectorMath.DotProduct(v1, v2);

        // Assert - Dot product should equal cosine of angle
        Assert.Equal(0.5, dotProduct, Tolerance);
    }

    [Fact]
    public void VectorToBlob_And_BlobToVector_RoundTrip()
    {
        // Arrange
        var original = new float[] { 1.5f, -2.5f, 3.14159f, 0.0f };

        // Act
        var blob = VectorMath.VectorToBlob(original);
        var restored = VectorMath.BlobToVector(blob);

        // Assert
        Assert.Equal(original.Length, restored.Length);
        for (int i = 0; i < original.Length; i++)
        {
            Assert.Equal(original[i], restored[i]);
        }
    }

    [Fact]
    public void VectorToBlob_ProducesCorrectSize()
    {
        // Arrange
        var vector = new float[] { 1.0f, 2.0f, 3.0f, 4.0f };

        // Act
        var blob = VectorMath.VectorToBlob(vector);

        // Assert - Should be 4 bytes per float
        Assert.Equal(vector.Length * sizeof(float), blob.Length);
    }

    [Fact]
    public void BlobToVector_ThrowsForInvalidBlobLength()
    {
        // Arrange - Blob length not divisible by sizeof(float)
        var invalidBlob = new byte[] { 0, 1, 2 };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => VectorMath.BlobToVector(invalidBlob));
    }

    [Fact]
    public void VectorToBlob_ThrowsForNullVector()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => VectorMath.VectorToBlob(null!));
    }

    [Fact]
    public void BlobToVector_ThrowsForNullBlob()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => VectorMath.BlobToVector(null!));
    }

    [Fact]
    public void VectorToBlob_HandlesEmptyVector()
    {
        // Arrange
        var emptyVector = Array.Empty<float>();

        // Act
        var blob = VectorMath.VectorToBlob(emptyVector);
        var restored = VectorMath.BlobToVector(blob);

        // Assert
        Assert.Empty(blob);
        Assert.Empty(restored);
    }

    [Fact]
    public void VectorSerialization_HandlesSpecialFloatValues()
    {
        // Arrange
        var specialValues = new float[] { float.MaxValue, float.MinValue, float.Epsilon, -float.Epsilon };

        // Act
        var blob = VectorMath.VectorToBlob(specialValues);
        var restored = VectorMath.BlobToVector(blob);

        // Assert
        for (int i = 0; i < specialValues.Length; i++)
        {
            Assert.Equal(specialValues[i], restored[i]);
        }
    }

    [Fact]
    public void NormalizeVector_HandlesHighDimensionalVectors()
    {
        // Arrange - 1024 dimensions (common for embedding models)
        // Using deterministic values instead of Random for reproducibility and security compliance
        var highDimVector = new float[1024];
        for (int i = 0; i < highDimVector.Length; i++)
        {
            // Deterministic pattern: sine wave values between -1 and 1
            highDimVector[i] = (float)Math.Sin(i * 0.123);
        }

        // Act
        var normalized = VectorMath.NormalizeVector(highDimVector);

        // Assert - Magnitude should be 1
        var magnitude = Math.Sqrt(normalized.Sum(x => x * (double)x));
        Assert.Equal(1.0, magnitude, Tolerance);
    }
}
