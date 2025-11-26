using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Config.Validation;

namespace KernelMemory.Core.Config.ContentIndex;

/// <summary>
/// SQLite content index configuration
/// </summary>
public sealed class SqliteContentIndexConfig : ContentIndexConfig
{
    /// <inheritdoc />
    [JsonIgnore]
    public override ContentIndexTypes Type => ContentIndexTypes.Sqlite;

    /// <summary>
    /// Path to SQLite database file (supports tilde expansion)
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <inheritdoc />
    public override void Validate(string path)
    {
        if (string.IsNullOrWhiteSpace(this.Path))
        {
            throw new ConfigException($"{path}.Path", "SQLite path is required");
        }

        // Path will be expanded and validated by the config loader
    }
}
