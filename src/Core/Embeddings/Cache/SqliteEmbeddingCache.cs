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
            key TEXT PRIMARY KEY,
            vector BLOB NOT NULL,
            token_count INTEGER NULL,
            timestamp TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS idx_timestamp ON embeddings_cache(timestamp);
        """;

    private const string SelectSql = "SELECT vector, token_count, timestamp FROM embeddings_cache WHERE key = @key";
    private const string UpsertSql = """
        INSERT INTO embeddings_cache (key, vector, token_count, timestamp) VALUES (@key, @vector, @tokenCount, @timestamp)
        ON CONFLICT(key) DO UPDATE SET vector = @vector, token_count = @tokenCount, timestamp = @timestamp
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

        var compositeKey = key.ToCompositeKey();

        var command = this._connection.CreateCommand();
        await using (command.ConfigureAwait(false))
        {
            command.CommandText = SelectSql;
            command.Parameters.AddWithValue("@key", compositeKey);

            var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                if (!await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    this._logger.LogTrace("Cache miss for key: {KeyPrefix}...", compositeKey[..Math.Min(50, compositeKey.Length)]);
                    return null;
                }

                var vectorBlob = (byte[])reader["vector"];
                var vector = BytesToFloatArray(vectorBlob);

                int? tokenCount = reader["token_count"] == DBNull.Value ? null : Convert.ToInt32(reader["token_count"], CultureInfo.InvariantCulture);
                var timestamp = DateTimeOffset.Parse((string)reader["timestamp"], CultureInfo.InvariantCulture);

                this._logger.LogTrace("Cache hit for key: {KeyPrefix}..., vector dimensions: {Dimensions}",
                    compositeKey[..Math.Min(50, compositeKey.Length)], vector.Length);

                return new CachedEmbedding
                {
                    Vector = vector,
                    TokenCount = tokenCount,
                    Timestamp = timestamp
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

        var compositeKey = key.ToCompositeKey();
        var vectorBlob = FloatArrayToBytes(vector);
        var timestamp = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture);

        var command = this._connection.CreateCommand();
        await using (command.ConfigureAwait(false))
        {
            command.CommandText = UpsertSql;
            command.Parameters.AddWithValue("@key", compositeKey);
            command.Parameters.AddWithValue("@vector", vectorBlob);
            command.Parameters.AddWithValue("@tokenCount", tokenCount.HasValue ? tokenCount.Value : DBNull.Value);
            command.Parameters.AddWithValue("@timestamp", timestamp);

            await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            this._logger.LogTrace("Stored embedding in cache: {KeyPrefix}..., vector dimensions: {Dimensions}",
                compositeKey[..Math.Min(50, compositeKey.Length)], vector.Length);
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
