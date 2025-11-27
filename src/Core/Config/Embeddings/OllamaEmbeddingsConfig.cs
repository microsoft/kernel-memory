// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Config.Validation;

namespace KernelMemory.Core.Config.Embeddings;

/// <summary>
/// Ollama embeddings provider configuration
/// </summary>
public sealed class OllamaEmbeddingsConfig : EmbeddingsConfig
{
    /// <inheritdoc />
    [JsonIgnore]
    public override EmbeddingsTypes Type => EmbeddingsTypes.Ollama;

    /// <summary>
    /// Ollama model name (e.g., "nomic-embed-text", "mxbai-embed-large")
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Ollama base URL (e.g., "http://localhost:11434")
    /// </summary>
    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <inheritdoc />
    public override void Validate(string path)
    {
        if (string.IsNullOrWhiteSpace(this.Model))
        {
            throw new ConfigException($"{path}.Model", "Ollama model name is required");
        }

        if (string.IsNullOrWhiteSpace(this.BaseUrl))
        {
            throw new ConfigException($"{path}.BaseUrl", "Ollama base URL is required");
        }

        if (!Uri.TryCreate(this.BaseUrl, UriKind.Absolute, out _))
        {
            throw new ConfigException($"{path}.BaseUrl",
                $"Invalid Ollama base URL: {this.BaseUrl}");
        }
    }
}
