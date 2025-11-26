using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Config.Validation;

namespace KernelMemory.Core.Config.Storage;

/// <summary>
/// Base class for storage configurations (files, repositories)
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(DiskStorageConfig), typeDiscriminator: "disk")]
[JsonDerivedType(typeof(AzureBlobStorageConfig), typeDiscriminator: "azureBlobs")]
public abstract class StorageConfig : IValidatable
{
    /// <summary>
    /// Type of storage backend
    /// </summary>
    [JsonIgnore]
    public abstract StorageTypes Type { get; }

    /// <summary>
    /// Validates the storage configuration
    /// </summary>
    /// <param name="path"></param>
    public abstract void Validate(string path);
}
