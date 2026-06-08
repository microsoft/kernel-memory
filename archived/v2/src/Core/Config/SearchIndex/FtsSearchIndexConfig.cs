// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Config.Validation;

namespace KernelMemory.Core.Config.SearchIndex;

/// <summary>
/// Full-Text Search index configuration (SQLite FTS5 or PostgreSQL)
/// </summary>
public sealed class FtsSearchIndexConfig : SearchIndexConfig
{
    /// <summary>
    /// Path to SQLite database (for SqliteFTS)
    /// Mutually exclusive with ConnectionString
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>
    /// PostgreSQL connection string (for PostgresFTS)
    /// Mutually exclusive with Path
    /// </summary>
    [JsonPropertyName("connectionString")]
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Enable stemming for better search results
    /// </summary>
    [JsonPropertyName("enableStemming")]
    public bool EnableStemming { get; set; } = true;

    /// <inheritdoc />
    public override void Validate(string path)
    {
        // Validate ID is provided
        if (string.IsNullOrWhiteSpace(this.Id))
        {
            throw new ConfigException($"{path}.Id", "Search index ID is required");
        }

        this.Embeddings?.Validate($"{path}.Embeddings");

        var isSqlite = this.Type == SearchIndexTypes.SqliteFTS;
        var isPostgres = this.Type == SearchIndexTypes.PostgresFTS;
        var hasPath = !string.IsNullOrWhiteSpace(this.Path);
        var hasConnectionString = !string.IsNullOrWhiteSpace(this.ConnectionString);

        if (isSqlite && !hasPath)
        {
            throw new ConfigException($"{path}.Path", "SQLite FTS requires Path");
        }

        if (isPostgres && !hasConnectionString)
        {
            throw new ConfigException($"{path}.ConnectionString",
                "PostgreSQL FTS requires ConnectionString");
        }

        if (hasPath && hasConnectionString)
        {
            throw new ConfigException(path,
                "FTS index: specify either Path (SQLite) or ConnectionString (Postgres), not both");
        }
    }
}
