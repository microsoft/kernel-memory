using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Validation;

namespace KernelMemory.Core.Config.SearchIndex;

/// <summary>
/// Graph-based search index configuration
/// Uses recursive CTEs for SQLite or Apache AGE for PostgreSQL
/// </summary>
public sealed class GraphSearchIndexConfig : SearchIndexConfig
{
    /// <summary>
    /// Path to SQLite database (for SQLite graph)
    /// Mutually exclusive with ConnectionString
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>
    /// PostgreSQL connection string (for Apache AGE graph)
    /// Mutually exclusive with Path
    /// </summary>
    [JsonPropertyName("connectionString")]
    public string? ConnectionString { get; set; }

    /// <inheritdoc />
    public override void Validate(string path)
    {
        this.Embeddings?.Validate($"{path}.Embeddings");

        var hasPath = !string.IsNullOrWhiteSpace(this.Path);
        var hasConnectionString = !string.IsNullOrWhiteSpace(this.ConnectionString);

        if (!hasPath && !hasConnectionString)
        {
            throw new ConfigException(path,
                "Graph index requires either Path (SQLite) or ConnectionString (Postgres)");
        }

        if (hasPath && hasConnectionString)
        {
            throw new ConfigException(path,
                "Graph index: specify either Path or ConnectionString, not both");
        }
    }
}
