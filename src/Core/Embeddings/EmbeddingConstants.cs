// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Embeddings;

/// <summary>
/// Constants for embedding generation including known model dimensions,
/// default configurations, and batch sizes.
/// </summary>
public static class EmbeddingConstants
{
    /// <summary>
    /// Default batch size for embedding generation requests.
    /// Configurable per provider, but this is the default.
    /// </summary>
    public const int DefaultBatchSize = 10;

    /// <summary>
    /// Default Ollama model for embeddings.
    /// </summary>
    public const string DefaultOllamaModel = "qwen3-embedding";

    /// <summary>
    /// Default Ollama base URL.
    /// </summary>
    public const string DefaultOllamaBaseUrl = "http://localhost:11434";

    /// <summary>
    /// Default HuggingFace model for embeddings.
    /// </summary>
    public const string DefaultHuggingFaceModel = "sentence-transformers/all-MiniLM-L6-v2";

    /// <summary>
    /// Default HuggingFace Inference API base URL.
    /// </summary>
    public const string DefaultHuggingFaceBaseUrl = "https://api-inference.huggingface.co";

    /// <summary>
    /// Default OpenAI API base URL.
    /// </summary>
    public const string DefaultOpenAIBaseUrl = "https://api.openai.com";

    /// <summary>
    /// Azure OpenAI API version.
    /// </summary>
    public const string AzureOpenAIApiVersion = "2024-02-01";

    /// <summary>
    /// Known model dimensions for common embedding models.
    /// These values are fixed per model and used for validation and cache key generation.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, int> KnownModelDimensions = new Dictionary<string, int>
    {
        // Ollama models
        ["qwen3-embedding"] = 1024,
        ["nomic-embed-text"] = 768,
        ["embeddinggemma"] = 768,

        // OpenAI models
        ["text-embedding-ada-002"] = 1536,
        ["text-embedding-3-small"] = 1536,
        ["text-embedding-3-large"] = 3072,

        // HuggingFace models
        ["sentence-transformers/all-MiniLM-L6-v2"] = 384,
        ["BAAI/bge-base-en-v1.5"] = 768
    };

    /// <summary>
    /// Try to get the dimensions for a known model.
    /// </summary>
    /// <param name="modelName">The model name to look up.</param>
    /// <param name="dimensions">The dimensions if found, 0 otherwise.</param>
    /// <returns>True if the model is known, false otherwise.</returns>
    public static bool TryGetDimensions(string modelName, out int dimensions)
    {
        return KnownModelDimensions.TryGetValue(modelName, out dimensions);
    }
}
