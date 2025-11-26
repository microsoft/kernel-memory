using System.Text.Json.Serialization;

namespace KernelMemory.Core.Config.Enums;

/// <summary>
/// Type of binary storage backend
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StorageTypes
{
    /// <summary>Local filesystem storage</summary>
    Disk,

    /// <summary>Azure Blob Storage</summary>
    AzureBlobs
}
