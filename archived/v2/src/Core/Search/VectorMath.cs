// Copyright (c) Microsoft. All rights reserved.
using System.Buffers.Binary;

namespace KernelMemory.Core.Search;

/// <summary>
/// Static utility class for vector mathematics operations.
/// Provides normalization, distance calculations, and serialization for vector search.
/// </summary>
public static class VectorMath
{
    /// <summary>
    /// Normalizes a vector to unit length (magnitude = 1).
    /// Normalized vectors allow dot product to be used for cosine similarity.
    /// </summary>
    /// <param name="vector">The vector to normalize.</param>
    /// <returns>A new normalized vector.</returns>
    /// <exception cref="ArgumentException">If the vector is zero-length or empty.</exception>
    /// <exception cref="ArgumentNullException">If vector is null.</exception>
    public static float[] NormalizeVector(float[] vector)
    {
        ArgumentNullException.ThrowIfNull(vector, nameof(vector));

        if (vector.Length == 0)
        {
            throw new ArgumentException("Cannot normalize empty vector", nameof(vector));
        }

        // Calculate magnitude (L2 norm)
        double sumOfSquares = 0.0;
        for (int i = 0; i < vector.Length; i++)
        {
            sumOfSquares += vector[i] * (double)vector[i];
        }

        var magnitude = Math.Sqrt(sumOfSquares);

        if (magnitude < double.Epsilon)
        {
            throw new ArgumentException("Cannot normalize zero vector", nameof(vector));
        }

        // Create normalized vector
        var normalized = new float[vector.Length];
        var magnitudeF = (float)magnitude;
        for (int i = 0; i < vector.Length; i++)
        {
            normalized[i] = vector[i] / magnitudeF;
        }

        return normalized;
    }

    /// <summary>
    /// Computes dot product of two vectors.
    /// For normalized vectors, this equals cosine similarity.
    /// </summary>
    /// <param name="a">First vector.</param>
    /// <param name="b">Second vector.</param>
    /// <returns>Dot product value (range -1 to 1 for normalized vectors).</returns>
    /// <exception cref="ArgumentException">If vectors have different lengths.</exception>
    /// <exception cref="ArgumentNullException">If either vector is null.</exception>
    public static double DotProduct(float[] a, float[] b)
    {
        ArgumentNullException.ThrowIfNull(a, nameof(a));
        ArgumentNullException.ThrowIfNull(b, nameof(b));

        if (a.Length != b.Length)
        {
            throw new ArgumentException($"Vectors must have same length: {a.Length} vs {b.Length}");
        }

        double sum = 0.0;
        for (int i = 0; i < a.Length; i++)
        {
            sum += a[i] * (double)b[i];
        }

        return sum;
    }

    /// <summary>
    /// Serializes a float32 vector to a byte array (BLOB).
    /// Uses little-endian format for cross-platform compatibility.
    /// </summary>
    /// <param name="vector">The vector to serialize.</param>
    /// <returns>Byte array representation.</returns>
    /// <exception cref="ArgumentNullException">If vector is null.</exception>
    public static byte[] VectorToBlob(float[] vector)
    {
        ArgumentNullException.ThrowIfNull(vector, nameof(vector));

        var blob = new byte[vector.Length * sizeof(float)];
        for (int i = 0; i < vector.Length; i++)
        {
            BinaryPrimitives.WriteSingleLittleEndian(blob.AsSpan(i * sizeof(float)), vector[i]);
        }

        return blob;
    }

    /// <summary>
    /// Deserializes a byte array (BLOB) to a float32 vector.
    /// Expects little-endian format.
    /// </summary>
    /// <param name="blob">The byte array to deserialize.</param>
    /// <returns>Float array representation.</returns>
    /// <exception cref="ArgumentNullException">If blob is null.</exception>
    /// <exception cref="ArgumentException">If blob length is not divisible by sizeof(float).</exception>
    public static float[] BlobToVector(byte[] blob)
    {
        ArgumentNullException.ThrowIfNull(blob, nameof(blob));

        if (blob.Length % sizeof(float) != 0)
        {
            throw new ArgumentException($"BLOB length {blob.Length} is not divisible by sizeof(float)", nameof(blob));
        }

        var vector = new float[blob.Length / sizeof(float)];
        for (int i = 0; i < vector.Length; i++)
        {
            vector[i] = BinaryPrimitives.ReadSingleLittleEndian(blob.AsSpan(i * sizeof(float)));
        }

        return vector;
    }
}
