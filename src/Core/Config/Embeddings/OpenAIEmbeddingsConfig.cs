// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Config.Validation;

namespace KernelMemory.Core.Config.Embeddings;

/// <summary>
/// OpenAI embeddings provider configuration
/// </summary>
public sealed class OpenAIEmbeddingsConfig : EmbeddingsConfig
{
    /// <inheritdoc />
    [JsonIgnore]
    public override EmbeddingsTypes Type => EmbeddingsTypes.OpenAI;

    /// <summary>
    /// OpenAI model name (e.g., "text-embedding-ada-002", "text-embedding-3-small")
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// OpenAI API key
    /// </summary>
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional custom base URL (for OpenAI-compatible APIs)
    /// </summary>
    [JsonPropertyName("baseUrl")]
    public string? BaseUrl { get; set; }

    /// <inheritdoc />
    public override void Validate(string path)
    {
        if (string.IsNullOrWhiteSpace(this.Model))
        {
            throw new ConfigException($"{path}.Model", "OpenAI model name is required");
        }

        if (string.IsNullOrWhiteSpace(this.ApiKey))
        {
            throw new ConfigException($"{path}.ApiKey", "OpenAI API key is required");
        }

        if (!string.IsNullOrWhiteSpace(this.BaseUrl) &&
            !Uri.TryCreate(this.BaseUrl, UriKind.Absolute, out _))
        {
            throw new ConfigException($"{path}.BaseUrl",
                $"Invalid OpenAI base URL: {this.BaseUrl}");
        }
    }
}
