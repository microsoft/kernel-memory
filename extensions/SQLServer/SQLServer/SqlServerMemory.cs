// Copyright (c) Microsoft. All rights reserved.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryDb.SQLServer.QueryProviders;
using Microsoft.KernelMemory.MemoryStorage;

namespace Microsoft.KernelMemory.MemoryDb.SQLServer;

/// <summary>
/// Represents a memory store implementation that uses a SQL Server database as its backing store.
/// </summary>
#pragma warning disable CA2100 // SQL reviewed for user input validation
public sealed class SqlServerMemory : IMemoryDb, IMemoryDbUpsertBatch, IDisposable
{
    /// <summary>
    /// The SQL Server configuration.
    /// </summary>
    private readonly SqlServerConfig _config;

    /// <summary>
    /// The text embedding generator.
    /// </summary>
    private readonly ITextEmbeddingGenerator _embeddingGenerator;

    /// <summary>
    /// The logger.
    /// </summary>
    private readonly ILogger<SqlServerMemory> _log;

    /// <summary>
    /// Flag used to initialize the client on the first call
    /// </summary>
    private bool _isReady = false;

    /// <summary>
    /// Lock used to initialize the class instance
    /// </summary>
    private readonly SemaphoreSlim _initSemaphore = new(1, 1);

    /// <summary>
    /// The interface that is responsible for getting queries against the SQL Server database.
    /// </summary>
    private readonly ISqlServerQueryProvider _queryProvider;

    /// <summary>
    /// SQL Server version, retrieved on the first connection
    /// </summary>
    private int _cachedServerVersion = int.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerMemory"/> class.
    /// </summary>
    /// <param name="config">The SQL server instance configuration.</param>
    /// <param name="embeddingGenerator">The text embedding generator.</param>
    /// <param name="queryProvider">SQL queries provider</param>
    /// <param name="loggerFactory">Application logger factory</param>
    public SqlServerMemory(
        SqlServerConfig config,
        ITextEmbeddingGenerator embeddingGenerator,
        ISqlServerQueryProvider? queryProvider = null,
        ILoggerFactory? loggerFactory = null)
    {
        this._config = config;
        this._embeddingGenerator = embeddingGenerator ?? throw new ConfigurationException("Embedding generator not configured");
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<SqlServerMemory>();
        this._queryProvider = queryProvider
                              ?? (this._config.UseNativeVectorSearch
                                  ? new VectorQueryProvider(this._config)
                                  : new DefaultQueryProvider(this._config));
    }

    /// <inheritdoc/>
    public async Task CreateIndexAsync(string index, int vectorSize, CancellationToken cancellationToken = default)
    {
        if (!this._isReady) { await this.InitAsync(cancellationToken).ConfigureAwait(false); }

        index = NormalizeIndexName(index);

        if (await this.DoesIndexExistsAsync(index, cancellationToken).ConfigureAwait(false))
        {
            // Index already exists
            return;
        }

        var sql = this._queryProvider.PrepareCreateIndexQuery(this._cachedServerVersion, index, vectorSize);

        var connection = new SqlConnection(this._config.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            SqlCommand command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@index", index);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            command.Dispose();
        }
        finally
        {
            await connection.CloseAsync().ConfigureAwait(false);
            connection.Dispose();
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        if (!this._isReady) { await this.InitAsync(cancellationToken).ConfigureAwait(false); }

        index = NormalizeIndexName(index);

        if (!await this.DoesIndexExistsAsync(index, cancellationToken).ConfigureAwait(false))
        {
            // Index does not exist
            return;
        }

        var sql = this._queryProvider.PrepareDeleteRecordQuery(index);

        var connection = new SqlConnection(this._config.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            SqlCommand command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@index", index);
            command.Parameters.AddWithValue("@key", record.Id);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            command.Dispose();
        }
        finally
        {
            await connection.CloseAsync().ConfigureAwait(false);
            connection.Dispose();
        }
    }

    /// <inheritdoc/>
    public async Task DeleteIndexAsync(string index, CancellationToken cancellationToken = default)
    {
        if (!this._isReady) { await this.InitAsync(cancellationToken).ConfigureAwait(false); }

        index = NormalizeIndexName(index);

        if (!await this.DoesIndexExistsAsync(index, cancellationToken).ConfigureAwait(false))
        {
            // Index does not exist
            return;
        }

        var sql = this._queryProvider.PrepareDeleteIndexQuery(index);

        var connection = new SqlConnection(this._config.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        SqlCommand command = connection.CreateCommand();
        try
        {
            command.CommandText = sql;
            command.Parameters.AddWithValue("@index", index);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            command.Dispose();
            await connection.CloseAsync().ConfigureAwait(false);
            connection.Dispose();
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetIndexesAsync(CancellationToken cancellationToken = default)
    {
        if (!this._isReady) { await this.InitAsync(cancellationToken).ConfigureAwait(false); }

        List<string> indexes = [];

        var sql = this._queryProvider.PrepareGetIndexesQuery();

        var connection = new SqlConnection(this._config.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        SqlCommand command = connection.CreateCommand();
        try
        {
            command.CommandText = sql;
            var dataReader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                indexes.Add(dataReader.GetString(dataReader.GetOrdinal("id")));
            }

            await dataReader.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            command.Dispose();
            await connection.CloseAsync().ConfigureAwait(false);
            connection.Dispose();
        }

        return indexes;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<MemoryRecord> GetListAsync(
        string index,
        ICollection<MemoryFilter>? filters = null,
        int limit = 1,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!this._isReady) { await this.InitAsync(cancellationToken).ConfigureAwait(false); }

        index = NormalizeIndexName(index);

        if (!await this.DoesIndexExistsAsync(index, cancellationToken).ConfigureAwait(false))
        {
            // Index does not exist
            yield break;
        }

        if (limit < 0) { limit = int.MaxValue; }

        var list = new List<MemoryRecord>();

        var connection = new SqlConnection(this._config.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        SqlCommand command = connection.CreateCommand();
        try
        {
            var tagFilters = new TagCollection();

            var sql = this._queryProvider.PrepareGetRecordsListQuery(index, filters, withEmbeddings, command.Parameters);
            command.CommandText = sql;

            command.Parameters.AddWithValue("@index", index);
            command.Parameters.AddWithValue("@limit", limit);
            command.Parameters.AddWithValue("@filters", JsonSerializer.Serialize(tagFilters));

            var dataReader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                // Iterates over the entries and saves them in a list so that the connection can be closed before returning the results.
                var entry = await this.ReadEntryAsync(dataReader, withEmbeddings, cancellationToken).ConfigureAwait(false);
                list.Add(entry);
            }

            await dataReader.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            command.Dispose();
            connection.Dispose();
        }

        foreach (var item in list)
        {
            yield return item;
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<(MemoryRecord, double)> GetSimilarListAsync(string index, string text, ICollection<MemoryFilter>? filters = null, double minRelevance = 0, int limit = 1, bool withEmbeddings = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!this._isReady) { await this.InitAsync(cancellationToken).ConfigureAwait(false); }

        index = NormalizeIndexName(index);

        if (!await this.DoesIndexExistsAsync(index, cancellationToken).ConfigureAwait(false))
        {
            // Index does not exist
            yield break;
        }

        Embedding embedding = await this._embeddingGenerator.GenerateEmbeddingAsync(text, cancellationToken).ConfigureAwait(false);

        var connection = new SqlConnection(this._config.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        SqlCommand command = connection.CreateCommand();
        try
        {
            var sql = this._queryProvider.PrepareGetSimilarRecordsListQuery(index, filters, withEmbeddings, command.Parameters);
            command.CommandText = sql;

            command.Parameters.AddWithValue("@min_relevance_score", minRelevance);
            command.Parameters.AddWithValue("@max_distance", 1 - minRelevance);
            command.Parameters.AddWithValue("@vector", JsonSerializer.Serialize(embedding.Data));
            command.Parameters.AddWithValue("@index", index);
            command.Parameters.AddWithValue("@limit", limit);

            var dataReader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                double cosineSimilarity = 0;
                if (this._config.UseNativeVectorSearch)
                {
                    double distance = dataReader.GetDouble(dataReader.GetOrdinal("distance"));
                    cosineSimilarity = 1 - distance;
                }
                else
                {
                    cosineSimilarity = dataReader.GetDouble(dataReader.GetOrdinal("cosine_similarity"));
                }

                yield return (await this.ReadEntryAsync(dataReader, withEmbeddings, cancellationToken).ConfigureAwait(false), cosineSimilarity);
            }

            await dataReader.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            command.Dispose();
            await connection.CloseAsync().ConfigureAwait(false);
            connection.Dispose();
        }
    }

    /// <inheritdoc/>
    public async Task<string> UpsertAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        if (!this._isReady) { await this.InitAsync(cancellationToken).ConfigureAwait(false); }

        await foreach (var item in this.UpsertBatchAsync(index, [record], cancellationToken).ConfigureAwait(false))
        {
            return item;
        }

        return null!;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> UpsertBatchAsync(string index, IEnumerable<MemoryRecord> records, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        List<MemoryRecord> list = records.ToList();
        this._log.LogDebug("Upserting records, batch size {0}", list.Count);

        if (!this._isReady) { await this.InitAsync(cancellationToken).ConfigureAwait(false); }

        index = NormalizeIndexName(index);

        if (!await this.DoesIndexExistsAsync(index, cancellationToken).ConfigureAwait(false))
        {
            throw new IndexNotFoundException($"The index '{index}' does not exist.");
        }

        var sql = this._queryProvider.PrepareUpsertRecordsBatchQuery(index);

        var connection = new SqlConnection(this._config.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            foreach (var record in list)
            {
                SqlCommand command = connection.CreateCommand();
                command.CommandText = sql;
                command.Parameters.AddWithValue("@index", index);
                command.Parameters.AddWithValue("@key", record.Id);
                command.Parameters.AddWithValue("@payload", JsonSerializer.Serialize(record.Payload) ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@tags", JsonSerializer.Serialize(record.Tags) ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@embedding", JsonSerializer.Serialize(record.Vector.Data));
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                command.Dispose();

                yield return record.Id;
            }
        }
        finally
        {
            await connection.CloseAsync().ConfigureAwait(false);
            connection.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this._initSemaphore.Dispose();
    }

    #region private ================================================================================

    // Note: "_" is allowed in SQL Server, but we normalize it to "-" for consistency with other DBs
    private static readonly Regex s_replaceIndexNameCharsRegex = new(@"[\s|\\|/|.|_|:]");
    private const string ValidSeparator = "-";

    /// <summary>
    /// Prepare instance, ensuring tables exist and reusable info is cached.
    /// </summary>
    private async Task InitAsync(CancellationToken cancellationToken)
    {
        if (this._isReady) { return; }

        var lockAcquired = false;
        try
        {
            await this._initSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            lockAcquired = true;

            if (this._isReady) { return; }

            await this.CacheSqlServerMajorVersionNumberAsync(cancellationToken).ConfigureAwait(false);
            await this.CreateTablesIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
            this._isReady = true;
        }
        finally
        {
            // Decrease the internal counter only it the lock was acquired,
            // e.g. not when WaitAsync times out or throws some exception
            if (lockAcquired)
            {
                this._initSemaphore.Release();
            }
        }
    }

    /// <summary>
    /// Cache SQL Server version
    /// </summary>
    private async Task CacheSqlServerMajorVersionNumberAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(this._config.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        SqlCommand command = connection.CreateCommand();

        try
        {
            command.CommandText = "SELECT SERVERPROPERTY('ProductMajorVersion')";
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            this._cachedServerVersion = Convert.ToInt32(result, CultureInfo.InvariantCulture);
        }
        finally
        {
            command.Dispose();
            await connection.CloseAsync().ConfigureAwait(false);
            connection.Dispose();
        }
    }

    /// <summary>
    /// Creates the SQL Server tables if they do not exist.
    /// </summary>
    /// <returns></returns>
    private async Task CreateTablesIfNotExistsAsync(CancellationToken cancellationToken)
    {
        var sql = this._queryProvider.PrepareCreateAllSupportingTablesQuery();

        var connection = new SqlConnection(this._config.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        SqlCommand command = connection.CreateCommand();
        try
        {
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            command.Dispose();
            await connection.CloseAsync().ConfigureAwait(false);
            connection.Dispose();
        }
    }

    /// <summary>
    /// Checks if the index exists.
    /// </summary>
    /// <param name="indexName">The index name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True is the index exists</returns>
    private async Task<bool> DoesIndexExistsAsync(string indexName,
        CancellationToken cancellationToken = default)
    {
        var collections = await this.GetIndexesAsync(cancellationToken).ConfigureAwait(false);
        return collections.Any(x => x.Equals(indexName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<MemoryRecord> ReadEntryAsync(SqlDataReader dataReader, bool withEmbedding, CancellationToken cancellationToken = default)
    {
        var entry = new MemoryRecord
        {
            Id = dataReader.GetString(dataReader.GetOrdinal("key"))
        };

        if (!await dataReader.IsDBNullAsync(dataReader.GetOrdinal("payload"), cancellationToken).ConfigureAwait(false))
        {
            entry.Payload = JsonSerializer.Deserialize<Dictionary<string, object>>(dataReader.GetString(dataReader.GetOrdinal("payload")))!;
        }

        if (!await dataReader.IsDBNullAsync(dataReader.GetOrdinal("tags"), cancellationToken).ConfigureAwait(false))
        {
            entry.Tags = JsonSerializer.Deserialize<TagCollection>(dataReader.GetString(dataReader.GetOrdinal("tags")))!;
        }

        if (withEmbedding)
        {
            entry.Vector = new ReadOnlyMemory<float>(JsonSerializer.Deserialize<IEnumerable<float>>(dataReader.GetString(dataReader.GetOrdinal("embedding")))!.ToArray());
        }

        return entry;
    }

    private static string NormalizeIndexName(string index)
    {
        ArgumentNullExceptionEx.ThrowIfNullOrWhiteSpace(index, nameof(index), "The index name is empty");

        index = s_replaceIndexNameCharsRegex.Replace(index.Trim().ToLowerInvariant(), ValidSeparator);

        return index;
    }

    #endregion
}
