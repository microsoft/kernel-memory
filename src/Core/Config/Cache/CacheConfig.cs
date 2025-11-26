using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Config.Validation;

namespace KernelMemory.Core.Config.Cache;

/// <summary>
/// Cache configuration for embeddings or LLM responses
/// </summary>
public sealed class CacheConfig : IValidatable
{
    /// <summary>
    /// Allow reading from cache
    /// </summary>
    [JsonPropertyName("allowRead")]
    public bool AllowRead { get; set; } = true;

    /// <summary>
    /// Allow writing to cache
    /// </summary>
    [JsonPropertyName("allowWrite")]
    public bool AllowWrite { get; set; } = true;

    /// <summary>
    /// Type of cache storage backend
    /// </summary>
    [JsonPropertyName("type")]
    public CacheTypes Type { get; set; } = CacheTypes.Sqlite;

    /// <summary>
    /// Path to SQLite database (for Sqlite cache)
    /// Mutually exclusive with ConnectionString
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>
    /// PostgreSQL connection string (for Postgres cache)
    /// Mutually exclusive with Path
    /// </summary>
    [JsonPropertyName("connectionString")]
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Validates the cache configuration
    /// </summary>
    /// <param name="path"></param>
    public void Validate(string path)
    {
        var isSqlite = this.Type == CacheTypes.Sqlite;
        var isPostgres = this.Type == CacheTypes.Postgres;
        var hasPath = !string.IsNullOrWhiteSpace(this.Path);
        var hasConnectionString = !string.IsNullOrWhiteSpace(this.ConnectionString);

        if (isSqlite && !hasPath)
        {
            throw new ConfigException($"{path}.Path", "SQLite cache requires Path");
        }

        if (isPostgres && !hasConnectionString)
        {
            throw new ConfigException($"{path}.ConnectionString",
                "PostgreSQL cache requires ConnectionString");
        }

        if (hasPath && hasConnectionString)
        {
            throw new ConfigException(path,
                "Cache: specify either Path (SQLite) or ConnectionString (Postgres), not both");
        }
    }

    /// <summary>
    /// Creates a default SQLite cache configuration
    /// </summary>
    /// <param name="path"></param>
    internal static CacheConfig CreateDefaultSqliteCache(string path)
    {
        return new CacheConfig
        {
            AllowRead = true,
            AllowWrite = true,
            Type = CacheTypes.Sqlite,
            Path = path,
            ConnectionString = null
        };
    }
}
