// Copyright (c) Microsoft. All rights reserved.
using System.Globalization;
using KernelMemory.Core.Config.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace KernelMemory.Core.Embeddings.Cache;

/// <summary>
/// SQLite-based embedding cache implementation.
/// Stores embedding vectors as BLOBs for efficient storage.
/// Uses WAL mode for better concurrency support.
/// </summary>
public sealed class SqliteEmbeddingCache : IEmbeddingCache, IDisposable
{
    private const string CreateTableSql = """
        CREATE TABLE IF NOT EXISTS embeddings_cache (
            provider TEXT NOT NULL,
            model TEXT NOT NULL,
            dimensions INTEGER NOT NULL,
            is_normalized INTEGER NOT NULL,
            text_length INTEGER NOT NULL,
            text_hash TEXT NOT NULL,
            vector BLOB NOT NULL,
            token_count INTEGER NULL,
            created_at TEXT NOT NULL,
            PRIMARY KEY (provider, model, dimensions, is_normalized, text_hash)
        );
        """;

    private const string SelectSql = """
        SELECT vector, token_count, created_at FROM embeddings_cache
        WHERE provider = @provider AND model = @model AND dimensions = @dimensions
        AND is_normalized = @isNormalized AND text_hash = @textHash
        """;

    private const string UpsertSql = """
        INSERT INTO embeddings_cache (provider, model, dimensions, is_normalized, text_length, text_hash, vector, token_count, created_at)
        VALUES (@provider, @model, @dimensions, @isNormalized, @textLength, @textHash, @vector, @tokenCount, @createdAt)
        ON CONFLICT(provider, model, dimensions, is_normalized, text_hash)
        DO UPDATE SET vector = @vector, token_count = @tokenCount, created_at = @createdAt
        """;

    private readonly SqliteConnection _connection;
    private readonly ILogger<SqliteEmbeddingCache> _logger;
    private bool _disposed;

    /// <inheritdoc />
    public CacheModes Mode { get; }

    /// <summary>
    /// Creates a new SQLite embedding cache.
    /// The database file is created if it doesn't exist.
    /// WAL mode is enabled for better concurrency.
    /// </summary>
    /// <param name="dbPath">Path to the SQLite database file.</param>
    /// <param name="mode">Cache mode (ReadWrite, ReadOnly, WriteOnly).</param>
    /// <param name="logger">Logger instance.</param>
    public SqliteEmbeddingCache(string dbPath, CacheModes mode, ILogger<SqliteEmbeddingCache> logger)
    {
        ArgumentNullException.ThrowIfNull(dbPath, nameof(dbPath));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        this.Mode = mode;
        this._logger = logger;

        // Ensure directory exists
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            this._logger.LogDebug("Created directory for embedding cache: {Directory}", directory);
        }

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        this._connection = new SqliteConnection(connectionString);
        this._connection.Open();

        // Enable WAL mode for better concurrency
        using var walCommand = this._connection.CreateCommand();
        walCommand.CommandText = "PRAGMA journal_mode=WAL;";
        walCommand.ExecuteNonQuery();

        // Set busy timeout to handle concurrent access
        using var busyCommand = this._connection.CreateCommand();
        busyCommand.CommandText = "PRAGMA busy_timeout=5000;";
        busyCommand.ExecuteNonQuery();

        // Create table if not exists
        using var createCommand = this._connection.CreateCommand();
        createCommand.CommandText = CreateTableSql;
        createCommand.ExecuteNonQuery();

        this._logger.LogInformation("Embedding cache initialized at {Path} with mode {Mode}", dbPath, mode);
    }

    /// <inheritdoc />
    public async Task<CachedEmbedding?> TryGetAsync(EmbeddingCacheKey key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // Skip read in WriteOnly mode
        if (this.Mode == CacheModes.WriteOnly)
        {
            this._logger.LogTrace("Skipping cache read in WriteOnly mode");
            return null;
        }

        var command = this._connection.CreateCommand();
        await using (command.ConfigureAwait(false))
        {
            command.CommandText = SelectSql;
            command.Parameters.AddWithValue("@provider", key.Provider);
            command.Parameters.AddWithValue("@model", key.Model);
            command.Parameters.AddWithValue("@dimensions", key.VectorDimensions);
            command.Parameters.AddWithValue("@isNormalized", key.IsNormalized ? 1 : 0);
            command.Parameters.AddWithValue("@textHash", key.TextHash);

            var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                if (!await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    this._logger.LogTrace("Cache miss for {Provider}/{Model} hash: {HashPrefix}...",
                        key.Provider, key.Model, key.TextHash[..Math.Min(16, key.TextHash.Length)]);
                    return null;
                }

                var vectorBlob = (byte[])reader["vector"];
                var vector = BytesToFloatArray(vectorBlob);

                int? tokenCount = reader["token_count"] == DBNull.Value ? null : Convert.ToInt32(reader["token_count"], CultureInfo.InvariantCulture);
                var createdAt = DateTimeOffset.Parse((string)reader["created_at"], CultureInfo.InvariantCulture);

                this._logger.LogTrace("Cache hit for {Provider}/{Model} hash: {HashPrefix}..., dimensions: {Dimensions}",
                    key.Provider, key.Model, key.TextHash[..Math.Min(16, key.TextHash.Length)], vector.Length);

                return new CachedEmbedding
                {
                    Vector = vector,
                    TokenCount = tokenCount,
                    Timestamp = createdAt
                };
            }
        }
    }

    /// <inheritdoc />
    public async Task StoreAsync(EmbeddingCacheKey key, float[] vector, int? tokenCount, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // Skip write in ReadOnly mode
        if (this.Mode == CacheModes.ReadOnly)
        {
            this._logger.LogTrace("Skipping cache write in ReadOnly mode");
            return;
        }

        var vectorBlob = FloatArrayToBytes(vector);
        var createdAt = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture);

        var command = this._connection.CreateCommand();
        await using (command.ConfigureAwait(false))
        {
            command.CommandText = UpsertSql;
            command.Parameters.AddWithValue("@provider", key.Provider);
            command.Parameters.AddWithValue("@model", key.Model);
            command.Parameters.AddWithValue("@dimensions", key.VectorDimensions);
            command.Parameters.AddWithValue("@isNormalized", key.IsNormalized ? 1 : 0);
            command.Parameters.AddWithValue("@textLength", key.TextLength);
            command.Parameters.AddWithValue("@textHash", key.TextHash);
            command.Parameters.AddWithValue("@vector", vectorBlob);
            command.Parameters.AddWithValue("@tokenCount", tokenCount.HasValue ? tokenCount.Value : DBNull.Value);
            command.Parameters.AddWithValue("@createdAt", createdAt);

            await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            this._logger.LogTrace("Stored embedding in cache: {Provider}/{Model} hash: {HashPrefix}..., dimensions: {Dimensions}",
                key.Provider, key.Model, key.TextHash[..Math.Min(16, key.TextHash.Length)], vector.Length);
        }
    }

    /// <summary>
    /// Converts a float array to a byte array for BLOB storage.
    /// </summary>
    private static byte[] FloatArrayToBytes(float[] array)
    {
        var bytes = new byte[array.Length * sizeof(float)];
        Buffer.BlockCopy(array, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    /// <summary>
    /// Converts a byte array from BLOB storage back to a float array.
    /// </summary>
    private static float[] BytesToFloatArray(byte[] bytes)
    {
        var array = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
        return array;
    }

    /// <summary>
    /// Disposes the SQLite connection.
    /// </summary>
    public void Dispose()
    {
        if (this._disposed) { return; }

        this._connection.Close();
        this._connection.Dispose();
        this._disposed = true;

        this._logger.LogDebug("Embedding cache disposed");
    }
}
