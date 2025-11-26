using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Config.Validation;

namespace KernelMemory.Core.Config.Storage;

/// <summary>
/// Azure Blob Storage configuration
/// </summary>
public sealed class AzureBlobStorageConfig : StorageConfig
{
    /// <inheritdoc />
    [JsonIgnore]
    public override StorageTypes Type => StorageTypes.AzureBlobs;

    /// <summary>
    /// Azure Storage connection string
    /// </summary>
    [JsonPropertyName("connectionString")]
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Azure Storage API key (alternative to connection string)
    /// </summary>
    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Use Azure Managed Identity for authentication
    /// </summary>
    [JsonPropertyName("useManagedIdentity")]
    public bool UseManagedIdentity { get; set; }

    /// <inheritdoc />
    public override void Validate(string path)
    {
        var hasConnectionString = !string.IsNullOrWhiteSpace(this.ConnectionString);
        var hasApiKey = !string.IsNullOrWhiteSpace(this.ApiKey);

        if (!hasConnectionString && !hasApiKey && !this.UseManagedIdentity)
        {
            throw new ConfigException(path,
                "Azure Blob storage requires one of: ConnectionString, ApiKey, or UseManagedIdentity");
        }

        if ((hasConnectionString ? 1 : 0) + (hasApiKey ? 1 : 0) + (this.UseManagedIdentity ? 1 : 0) > 1)
        {
            throw new ConfigException(path,
                "Azure Blob storage: specify only one authentication method");
        }
    }
}
