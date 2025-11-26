using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Config.Validation;

namespace KernelMemory.Core.Config.Embeddings;

/// <summary>
/// Azure OpenAI embeddings provider configuration
/// </summary>
public sealed class AzureOpenAIEmbeddingsConfig : EmbeddingsConfig
{
    /// <inheritdoc />
    [JsonIgnore]
    public override EmbeddingsTypes Type => EmbeddingsTypes.AzureOpenAI;

    /// <summary>
    /// Model name (e.g., "text-embedding-ada-002")
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Azure OpenAI endpoint (e.g., "https://myservice.openai.azure.com/")
    /// </summary>
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Azure OpenAI API key (optional if using managed identity)
    /// </summary>
    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Deployment name in Azure OpenAI
    /// </summary>
    [JsonPropertyName("deployment")]
    public string Deployment { get; set; } = string.Empty;

    /// <summary>
    /// Use Azure Managed Identity for authentication
    /// </summary>
    [JsonPropertyName("useManagedIdentity")]
    public bool UseManagedIdentity { get; set; }

    /// <inheritdoc />
    public override void Validate(string path)
    {
        if (string.IsNullOrWhiteSpace(this.Model))
        {
            throw new ConfigException($"{path}.Model", "Model name is required");
        }

        if (string.IsNullOrWhiteSpace(this.Endpoint))
        {
            throw new ConfigException($"{path}.Endpoint", "Azure OpenAI endpoint is required");
        }

        if (!Uri.TryCreate(this.Endpoint, UriKind.Absolute, out _))
        {
            throw new ConfigException($"{path}.Endpoint",
                $"Invalid Azure OpenAI endpoint: {this.Endpoint}");
        }

        if (string.IsNullOrWhiteSpace(this.Deployment))
        {
            throw new ConfigException($"{path}.Deployment", "Deployment name is required");
        }

        var hasApiKey = !string.IsNullOrWhiteSpace(this.ApiKey);

        if (!hasApiKey && !this.UseManagedIdentity)
        {
            throw new ConfigException(path,
                "Azure OpenAI requires either ApiKey or UseManagedIdentity");
        }

        if (hasApiKey && this.UseManagedIdentity)
        {
            throw new ConfigException(path,
                "Azure OpenAI: specify either ApiKey or UseManagedIdentity, not both");
        }
    }
}
