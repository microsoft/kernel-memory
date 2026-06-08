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

        // Set synchronous=FULL to ensure writes are immediately persisted to disk
        // This prevents data loss when connections are disposed quickly (CLI scenario)
        using (var pragmaCmd = this._connection.CreateCommand())
        {
            pragmaCmd.CommandText = "PRAGMA synchronous=FULL;";
            await pragmaCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        // Create FTS5 virtual table if it doesn't exist
        // BREAKING CHANGE: New schema indexes title, description, content separately
        // This enables field-specific search (e.g., title:kubernetes vs content:kubernetes)
        // Using regular FTS5 table (stores content) to support snippets
        var tokenizer = this._enableStemming ? "porter unicode61" : "unicode61";
        var createTableSql = $"""
            CREATE VIRTUAL TABLE IF NOT EXISTS {TableName} USING fts5(
                content_id UNINDEXED,
                title,
                description,
                content,
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
        // Legacy method - indexes text as content only (no title/description)
        await this.IndexAsync(contentId, null, null, text, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Indexes content with separate FTS-indexed fields.
    /// BREAKING CHANGE: New signature to support title, description, content separately.
    /// </summary>
    /// <param name="contentId">Unique content identifier.</param>
    /// <param name="title">Optional title (FTS-indexed).</param>
    /// <param name="description">Optional description (FTS-indexed).</param>
    /// <param name="content">Main content body (FTS-indexed, required).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task IndexAsync(string contentId, string? title, string? description, string content, CancellationToken cancellationToken = default)
    {
        await this.InitializeAsync(cancellationToken).ConfigureAwait(false);

        // Remove existing entry first (upsert semantics)
        await this.RemoveAsync(contentId, cancellationToken).ConfigureAwait(false);

        // Insert new entry with separate fields
        var insertSql = $"INSERT INTO {TableName}(content_id, title, description, content) VALUES (@contentId, @title, @description, @content)";

        var insertCommand = this._connection!.CreateCommand();
        await using (insertCommand.ConfigureAwait(false))
        {
            insertCommand.CommandText = insertSql;
            insertCommand.Parameters.AddWithValue("@contentId", contentId);
            insertCommand.Parameters.AddWithValue("@title", title ?? string.Empty);
            insertCommand.Parameters.AddWithValue("@description", description ?? string.Empty);
            insertCommand.Parameters.AddWithValue("@content", content);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        this._logger.LogDebug("Indexed content {ContentId} with title={HasTitle}, description={HasDescription} in FTS",
            contentId, !string.IsNullOrEmpty(title), !string.IsNullOrEmpty(description));
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

        // Handle special "*" query to return all documents (for standalone NOT queries)
        // FTS5 doesn't support "*" alone as a match-all operator
        // Used when query contains only NOT terms - we get all docs and filter externally
        if (query == "*")
        {
            return await this.GetAllDocumentsAsync(limit, cancellationToken).ConfigureAwait(false);
        }

        // Search using FTS5 MATCH operator
        // Use bm25() for better scoring (returns negative values, more negative = better match)
        // We negate and normalize to 0-1 range
        // snippet() generates highlighted text excerpts from the content field (column index 3)
        var searchSql = $"""
            SELECT
                content_id,
                bm25({TableName}) as raw_score,
                snippet({TableName}, 3, '', '', '...', 32) as snippet
            FROM {TableName}
            WHERE {TableName} MATCH @query
            ORDER BY raw_score
            LIMIT @limit
            """;

        var searchCommand = this._connection!.CreateCommand();
        await using (searchCommand.ConfigureAwait(false))
        {
            searchCommand.CommandText = searchSql;
            searchCommand.Parameters.AddWithValue("@query", query);
            searchCommand.Parameters.AddWithValue("@limit", limit);

            var rawResults = new List<(string ContentId, double RawScore, string Snippet)>();
            var reader = await searchCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var contentId = reader.GetString(0);
                    var rawScore = reader.GetDouble(1);
                    var snippet = reader.GetString(2);
                    rawResults.Add((contentId, rawScore, snippet));
                }
            }

            // Normalize BM25 scores to 0-1 range
            // BM25 returns negative scores where more negative = better match
            // Convert to positive scores using exponential normalization
            var results = new List<FtsMatch>();
            foreach (var (contentId, rawScore, snippet) in rawResults)
            {
                // BM25 scores are typically in range [-10, 0]
                // Use exponential function to map to [0, 1]: score = exp(raw_score / divisor)
                // This gives: -10 → 0.37, -5 → 0.61, -1 → 0.90, 0 → 1.0
                var normalizedScore = Math.Exp(rawScore / Constants.SearchDefaults.Bm25NormalizationDivisor);

                results.Add(new FtsMatch
                {
                    ContentId = contentId,
                    Score = normalizedScore,
                    Snippet = snippet
                });
            }

            this._logger.LogDebug("FTS search for '{Query}' returned {Count} results", query, results.Count);
            return results;
        }
    }

    /// <summary>
    /// Returns all documents from the FTS index without filtering.
    /// Used for standalone NOT queries where we need to get all documents
    /// and then filter externally using LINQ.
    /// </summary>
    /// <param name="limit">Maximum number of documents to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All documents up to the limit.</returns>
    private async Task<IReadOnlyList<FtsMatch>> GetAllDocumentsAsync(int limit, CancellationToken cancellationToken)
    {
        // Select all documents without FTS MATCH filtering
        // Since there's no FTS query, we can't use bm25() - assign a default score of 1.0
        var searchSql = "SELECT content_id, substr(content, 1, " + Constants.Database.DefaultSqlSnippetLength + ") as snippet FROM " + TableName + " LIMIT @limit";

        var searchCommand = this._connection!.CreateCommand();
        await using (searchCommand.ConfigureAwait(false))
        {
#pragma warning disable CA2100 // SQL string uses only constants and table name - no user input
            searchCommand.CommandText = searchSql;
#pragma warning restore CA2100
            searchCommand.Parameters.AddWithValue("@limit", limit);

            var results = new List<FtsMatch>();
            var reader = await searchCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var contentId = reader.GetString(0);
                    var snippet = reader.GetString(1);

                    results.Add(new FtsMatch
                    {
                        ContentId = contentId,
                        Score = 1.0, // Default score for match-all
                        Snippet = snippet
                    });
                }
            }

            this._logger.LogDebug("GetAllDocuments returned {Count} results", results.Count);
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
    /// Ensures all pending writes are flushed to disk before closing.
    /// </summary>
    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        // Flush any pending writes before closing the connection
        // SQLite needs explicit close to ensure writes are persisted
        if (this._connection != null)
        {
            try
            {
                // Execute a checkpoint to flush WAL to disk (if WAL mode is enabled)
                using var cmd = this._connection.CreateCommand();
                cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                cmd.ExecuteNonQuery();
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex)
            {
                this._logger.LogWarning(ex, "Failed to checkpoint WAL during FTS index disposal");
            }
            catch (InvalidOperationException ex)
            {
                this._logger.LogWarning(ex, "Failed to checkpoint WAL during FTS index disposal - connection in invalid state");
            }

            this._connection.Close();
            this._connection.Dispose();
            this._connection = null;
        }

        this._disposed = true;
    }
}
