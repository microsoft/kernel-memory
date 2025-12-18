// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Config.Enums;

namespace KernelMemory.Core.Embeddings;

/// <summary>
/// Interface for generating text embeddings using various providers.
/// Implementations handle communication with embedding APIs (OpenAI, Azure, Ollama, HuggingFace).
/// </summary>
public interface IEmbeddingGenerator
{
    /// <summary>
    /// Provider type (OpenAI, Azure, Ollama, HuggingFace).
    /// Used for cache key generation to ensure embeddings from different providers are not mixed.
    /// </summary>
    EmbeddingsTypes ProviderType { get; }

    /// <summary>
    /// Model name being used for embedding generation.
    /// Used for cache key generation to ensure embeddings from different models are not mixed.
    /// </summary>
    string ModelName { get; }

    /// <summary>
    /// Vector dimensions produced by this model.
    /// Used for validation and cache key generation.
    /// </summary>
    int VectorDimensions { get; }

    /// <summary>
    /// Whether this generator returns normalized vectors.
    /// Normalized vectors have unit length (magnitude 1.0).
    /// Used for cache key generation since normalized and non-normalized vectors are not interchangeable.
    /// </summary>
    bool IsNormalized { get; }

    /// <summary>
    /// Generate embedding for a single text.
    /// </summary>
    /// <param name="text">The text to generate embedding for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The embedding result with vector and optional token count.</returns>
    /// <exception cref="HttpRequestException">When the API call fails.</exception>
    /// <exception cref="OperationCanceledException">When the operation is cancelled.</exception>
    Task<EmbeddingResult> GenerateAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Generate embeddings for multiple texts (batch).
    /// Implementations may send texts in batches to the API based on provider-specific limits.
    /// </summary>
    /// <param name="texts">The texts to generate embeddings for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Array of embedding results with vectors and optional token counts, in the same order as the input texts.</returns>
    /// <exception cref="HttpRequestException">When the API call fails.</exception>
    /// <exception cref="OperationCanceledException">When the operation is cancelled.</exception>
    Task<EmbeddingResult[]> GenerateAsync(IEnumerable<string> texts, CancellationToken ct = default);
}
