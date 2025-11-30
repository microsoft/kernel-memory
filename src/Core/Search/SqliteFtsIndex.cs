// Copyright (c) Microsoft. All rights reserved.
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace KernelMemory.Core.Search;

/// <summary>
/// SQLite FTS5 implementation of IFtsIndex.
/// Uses a contentless FTS5 table for efficient full-text search.
/// </summary>
public sealed class SqliteFtsIndex : IFtsIndex, IDisposable
{
    private const string TableName = "km_fts";
    private readonly string _connectionString;
    private readonly bool _enableStemming;
    private readonly ILogger<SqliteFtsIndex> _logger;
    private SqliteConnection? _connection;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of SqliteFtsIndex.
    /// </summary>
    /// <param name="dbPath">Path to the SQLite database file.</param>
    /// <param name="enableStemming">Enable Porter stemming for better search results.</param>
    /// <param name="logger">Logger instance.</param>
    public SqliteFtsIndex(string dbPath, bool enableStemming, ILogger<SqliteFtsIndex> logger)
    {
        this._connectionString = $"Data Source={dbPath}";
        this._enableStemming = enableStemming;
        this._logger = logger;
    }

    /// <summary>
    /// Ensures the database connection is open and tables exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (this._connection != null)
        {
            return;
        }

        this._connection = new SqliteConnection(this._connectionString);
        await this._connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // Create FTS5 virtual table if it doesn't exist
        // Using regular FTS5 table (stores content) to support snippets
        var tokenizer = this._enableStemming ? "porter unicode61" : "unicode61";
        var createTableSql = $"""
            CREATE VIRTUAL TABLE IF NOT EXISTS {TableName} USING fts5(
                content_id UNINDEXED,
                text,
                tokenize='{tokenizer}'
            );
            """;

        var command = this._connection.CreateCommand();
        await using (command.ConfigureAwait(false))
        {
            command.CommandText = createTableSql;
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        this._logger.LogDebug("FTS5 index initialized at {ConnectionString}", this._connectionString);
    }

    /// <inheritdoc />
    public async Task IndexAsync(string contentId, string text, CancellationToken cancellationToken = default)
    {
        await this.InitializeAsync(cancellationToken).ConfigureAwait(false);

        // Remove existing entry first (upsert semantics)
        await this.RemoveAsync(contentId, cancellationToken).ConfigureAwait(false);

        // Insert new entry
        var insertSql = $"INSERT INTO {TableName}(content_id, text) VALUES (@contentId, @text)";

        var insertCommand = this._connection!.CreateCommand();
        await using (insertCommand.ConfigureAwait(false))
        {
            insertCommand.CommandText = insertSql;
            insertCommand.Parameters.AddWithValue("@contentId", contentId);
            insertCommand.Parameters.AddWithValue("@text", text);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        this._logger.LogDebug("Indexed content {ContentId} in FTS", contentId);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string contentId, CancellationToken cancellationToken = default)
    {
        await this.InitializeAsync(cancellationToken).ConfigureAwait(false);

        var deleteSql = $"DELETE FROM {TableName} WHERE content_id = @contentId";

        var deleteCommand = this._connection!.CreateCommand();
        await using (deleteCommand.ConfigureAwait(false))
        {
            deleteCommand.CommandText = deleteSql;
            deleteCommand.Parameters.AddWithValue("@contentId", contentId);
            var rowsAffected = await deleteCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            if (rowsAffected > 0)
            {
                this._logger.LogDebug("Removed content {ContentId} from FTS index", contentId);
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FtsMatch>> SearchAsync(string query, int limit = 10, CancellationToken cancellationToken = default)
    {
        await this.InitializeAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        // Search using FTS5 MATCH operator
        // rank is negative (closer to 0 is better), so we negate it for Score
        // snippet() generates highlighted text excerpts
        var searchSql = $"""
            SELECT 
                content_id,
                -rank as score,
                snippet({TableName}, 1, '', '', '...', 32) as snippet
            FROM {TableName}
            WHERE {TableName} MATCH @query
            ORDER BY rank
            LIMIT @limit
            """;

        var searchCommand = this._connection!.CreateCommand();
        await using (searchCommand.ConfigureAwait(false))
        {
            searchCommand.CommandText = searchSql;
            searchCommand.Parameters.AddWithValue("@query", query);
            searchCommand.Parameters.AddWithValue("@limit", limit);

            var results = new List<FtsMatch>();
            var reader = await searchCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    results.Add(new FtsMatch
                    {
                        ContentId = reader.GetString(0),
                        Score = reader.GetDouble(1),
                        Snippet = reader.GetString(2)
                    });
                }
            }

            this._logger.LogDebug("FTS search for '{Query}' returned {Count} results", query, results.Count);
            return results;
        }
    }

    /// <inheritdoc />
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await this.InitializeAsync(cancellationToken).ConfigureAwait(false);

        var deleteSql = $"DELETE FROM {TableName}";

        var clearCommand = this._connection!.CreateCommand();
        await using (clearCommand.ConfigureAwait(false))
        {
            clearCommand.CommandText = deleteSql;
            await clearCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        this._logger.LogInformation("Cleared all entries from FTS index");
    }

    /// <summary>
    /// Disposes the database connection.
    /// </summary>
    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        this._connection?.Dispose();
        this._connection = null;
        this._disposed = true;
    }
}
