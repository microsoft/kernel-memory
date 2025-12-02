// Copyright (c) Microsoft. All rights reserved.
using System.Security.Cryptography;
using System.Text;

namespace KernelMemory.Core.Embeddings.Cache;

/// <summary>
/// Cache key for embeddings. Uniquely identifies an embedding by provider, model,
/// dimensions, normalization state, and content hash.
/// The input text is NOT stored - only a SHA256 hash is used for security.
/// </summary>
public sealed class EmbeddingCacheKey
{
    /// <summary>
    /// Provider type name (e.g., "OpenAI", "Ollama", "AzureOpenAI", "HuggingFace").
    /// </summary>
    public required string Provider { get; init; }

    /// <summary>
    /// Model name (e.g., "text-embedding-ada-002", "qwen3-embedding").
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// Vector dimensions produced by this model.
    /// </summary>
    public required int VectorDimensions { get; init; }

    /// <summary>
    /// Whether the vectors are normalized.
    /// </summary>
    public required bool IsNormalized { get; init; }

    /// <summary>
    /// Length of the original text in characters.
    /// Used as an additional collision prevention measure.
    /// </summary>
    public required int TextLength { get; init; }

    /// <summary>
    /// SHA256 hash of the original text (hex string).
    /// The text itself is never stored for security/privacy.
    /// </summary>
    public required string TextHash { get; init; }

    /// <summary>
    /// Creates a cache key from the given parameters.
    /// The text is hashed using SHA256 and not stored.
    /// </summary>
    /// <param name="provider">Provider type name.</param>
    /// <param name="model">Model name.</param>
    /// <param name="vectorDimensions">Vector dimensions.</param>
    /// <param name="isNormalized">Whether vectors are normalized.</param>
    /// <param name="text">The text to hash.</param>
    /// <returns>A new EmbeddingCacheKey instance.</returns>
    /// <exception cref="ArgumentNullException">When provider, model, or text is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">When vectorDimensions is less than 1.</exception>
    public static EmbeddingCacheKey Create(
        string provider,
        string model,
        int vectorDimensions,
        bool isNormalized,
        string text)
    {
        ArgumentNullException.ThrowIfNull(provider, nameof(provider));
        ArgumentNullException.ThrowIfNull(model, nameof(model));
        ArgumentNullException.ThrowIfNull(text, nameof(text));
        ArgumentOutOfRangeException.ThrowIfLessThan(vectorDimensions, 1, nameof(vectorDimensions));

        return new EmbeddingCacheKey
        {
            Provider = provider,
            Model = model,
            VectorDimensions = vectorDimensions,
            IsNormalized = isNormalized,
            TextLength = text.Length,
            TextHash = ComputeSha256Hash(text)
        };
    }

    /// <summary>
    /// Generates a composite key string for use as a database primary key.
    /// Format: Provider|Model|Dimensions|IsNormalized|TextLength|TextHash
    /// </summary>
    /// <returns>A string suitable for use as a cache key.</returns>
    public string ToCompositeKey()
    {
        return $"{this.Provider}|{this.Model}|{this.VectorDimensions}|{this.IsNormalized}|{this.TextLength}|{this.TextHash}";
    }

    /// <summary>
    /// Computes SHA256 hash of the input text and returns as lowercase hex string.
    /// </summary>
    /// <param name="text">The text to hash.</param>
    /// <returns>64-character lowercase hex string.</returns>
    private static string ComputeSha256Hash(string text)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexStringLower(bytes);
    }
}
