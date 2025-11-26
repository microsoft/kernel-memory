using System.Text.Json.Serialization;

namespace KernelMemory.Core.Config.Enums;

/// <summary>
/// Type of search index for content retrieval
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SearchIndexTypes
{
    /// <summary>SQLite Full-Text Search (FTS5)</summary>
    SqliteFTS,

    /// <summary>PostgreSQL Full-Text Search</summary>
    PostgresFTS,

    /// <summary>SQLite with vector extensions (sqlite-vec)</summary>
    SqliteVector,

    /// <summary>PostgreSQL with pgvector extension</summary>
    PostgresVector,

    /// <summary>Graph-based semantic search</summary>
    Graph
}
