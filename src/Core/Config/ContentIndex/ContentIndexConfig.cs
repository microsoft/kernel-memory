using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Config.Validation;

namespace KernelMemory.Core.Config.ContentIndex;

/// <summary>
/// Base class for content index configurations
/// Content index is the source of truth, backed by Entity Framework
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(SqliteContentIndexConfig), typeDiscriminator: "sqlite")]
[JsonDerivedType(typeof(PostgresContentIndexConfig), typeDiscriminator: "postgres")]
public abstract class ContentIndexConfig : IValidatable
{
    /// <summary>
    /// Type of content index
    /// </summary>
    [JsonIgnore]
    public abstract ContentIndexTypes Type { get; }

    /// <summary>
    /// Validates the content index configuration
    /// </summary>
    /// <param name="path"></param>
    public abstract void Validate(string path);
}
