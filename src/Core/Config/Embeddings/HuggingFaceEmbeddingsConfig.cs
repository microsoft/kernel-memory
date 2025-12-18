// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Config.Validation;

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
    public string Model { get; set; } = Constants.EmbeddingDefaults.DefaultHuggingFaceModel;

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
    public string BaseUrl { get; set; } = Constants.EmbeddingDefaults.DefaultHuggingFaceBaseUrl;

    /// <inheritdoc />
    public override void Validate(string path)
    {
        if (string.IsNullOrWhiteSpace(this.Model))
        {
            throw new ConfigException($"{path}.Model", "HuggingFace model name is required");
        }

        // ApiKey can be provided via config or HF_TOKEN environment variable.
        // Resolve env var into config so downstream code can rely on configuration only.
        if (string.IsNullOrWhiteSpace(this.ApiKey))
        {
            var token = Environment.GetEnvironmentVariable("HF_TOKEN");
            if (!string.IsNullOrWhiteSpace(token))
            {
                this.ApiKey = token;
            }
        }

        if (string.IsNullOrWhiteSpace(this.ApiKey))
        {
            throw new ConfigException($"{path}.ApiKey", "HuggingFace API key is required (set ApiKey or HF_TOKEN)");
        }

        if (this.BatchSize < 1)
        {
            throw new ConfigException($"{path}.BatchSize", "BatchSize must be >= 1");
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
