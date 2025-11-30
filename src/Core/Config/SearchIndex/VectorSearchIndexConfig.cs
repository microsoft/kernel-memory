// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Config.Validation;

namespace KernelMemory.Core.Config.SearchIndex;

/// <summary>
/// Vector search index configuration (SQLite with sqlite-vec or PostgreSQL with pgvector)
/// </summary>
public sealed class VectorSearchIndexConfig : SearchIndexConfig
{
    /// <summary>
    /// Path to SQLite database (for SqliteVector)
    /// Mutually exclusive with ConnectionString
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>
    /// PostgreSQL connection string (for PostgresVector)
    /// Mutually exclusive with Path
    /// </summary>
    [JsonPropertyName("connectionString")]
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Vector dimensions (must match embeddings model)
    /// Common values: 384 (MiniLM), 768 (BERT), 1536 (OpenAI ada-002), 3072 (OpenAI ada-003)
    /// </summary>
    [JsonPropertyName("dimensions")]
    public int Dimensions { get; set; } = 768;

    /// <summary>
    /// Distance/similarity metric for vector comparison
    /// </summary>
    [JsonPropertyName("metric")]
    public VectorMetrics Metric { get; set; } = VectorMetrics.Cosine;

    /// <inheritdoc />
    public override void Validate(string path)
    {
        // Validate ID is provided
        if (string.IsNullOrWhiteSpace(this.Id))
        {
            throw new ConfigException($"{path}.Id", "Search index ID is required");
        }

        this.Embeddings?.Validate($"{path}.Embeddings");

        var isSqlite = this.Type == SearchIndexTypes.SqliteVector;
        var isPostgres = this.Type == SearchIndexTypes.PostgresVector;
        var hasPath = !string.IsNullOrWhiteSpace(this.Path);
        var hasConnectionString = !string.IsNullOrWhiteSpace(this.ConnectionString);

        if (isSqlite && !hasPath)
        {
            throw new ConfigException($"{path}.Path", "SQLite vector index requires Path");
        }

        if (isPostgres && !hasConnectionString)
        {
            throw new ConfigException($"{path}.ConnectionString",
                "PostgreSQL vector index requires ConnectionString");
        }

        if (hasPath && hasConnectionString)
        {
            throw new ConfigException(path,
                "Vector index: specify either Path (SQLite) or ConnectionString (Postgres), not both");
        }

        if (this.Dimensions <= 0)
        {
            throw new ConfigException($"{path}.Dimensions",
                $"Vector dimensions must be positive (got {this.Dimensions})");
        }

        // Common dimensions check (warning, not error)
        var commonDimensions = new[] { 384, 768, 1024, 1536, 3072 };
        if (!commonDimensions.Contains(this.Dimensions))
        {
            // Log warning: uncommon dimension size
            // This is acceptable, just informational
        }
    }
}
