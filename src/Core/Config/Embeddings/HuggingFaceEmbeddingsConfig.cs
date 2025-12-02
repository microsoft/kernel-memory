// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Config.Validation;
using KernelMemory.Core.Embeddings;

namespace KernelMemory.Core.Config.Embeddings;

/// <summary>
/// HuggingFace Inference API embeddings provider configuration.
/// Supports the serverless Inference API for embedding models.
/// </summary>
public sealed class HuggingFaceEmbeddingsConfig : EmbeddingsConfig
{
    /// <inheritdoc />
    [JsonIgnore]
    public override EmbeddingsTypes Type => EmbeddingsTypes.HuggingFace;

    /// <summary>
    /// HuggingFace model name (e.g., "sentence-transformers/all-MiniLM-L6-v2", "BAAI/bge-base-en-v1.5").
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = EmbeddingConstants.DefaultHuggingFaceModel;

    /// <summary>
    /// HuggingFace API key (token).
    /// Can also be set via HF_TOKEN environment variable.
    /// </summary>
    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }

    /// <summary>
    /// HuggingFace Inference API base URL.
    /// Default: https://api-inference.huggingface.co
    /// Can be changed for custom inference endpoints.
    /// </summary>
    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; } = EmbeddingConstants.DefaultHuggingFaceBaseUrl;

    /// <inheritdoc />
    public override void Validate(string path)
    {
        if (string.IsNullOrWhiteSpace(this.Model))
        {
            throw new ConfigException($"{path}.Model", "HuggingFace model name is required");
        }

        if (string.IsNullOrWhiteSpace(this.ApiKey))
        {
            throw new ConfigException($"{path}.ApiKey", "HuggingFace API key is required");
        }

        if (string.IsNullOrWhiteSpace(this.BaseUrl))
        {
            throw new ConfigException($"{path}.BaseUrl", "HuggingFace base URL is required");
        }

        if (!Uri.TryCreate(this.BaseUrl, UriKind.Absolute, out _))
        {
            throw new ConfigException($"{path}.BaseUrl",
                $"Invalid HuggingFace base URL: {this.BaseUrl}");
        }
    }
}
