// Copyright (c) Microsoft. All rights reserved.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryStorage;

namespace Microsoft.KernelMemory.MemoryDb.SQLServer;

/// <summary>
/// Represents a memory store implementation that uses a SQL Server database as its backing store.
/// </summary>
#pragma warning disable CA2100 // SQL reviewed for user input validation
public class SqlServerMemory : IMemoryDb, IMemoryDbBatchUpsert
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
    /// Initializes a new instance of the <see cref="SqlServerMemory"/> class.
    /// </summary>
    /// <param name="config">The SQL server instance configuration.</param>
    /// <param name="embeddingGenerator">The text embedding generator.</param>
    /// <param name="log">The logger.</param>
    public SqlServerMemory(
        SqlServerConfig config,
        ITextEmbeddingGenerator embeddingGenerator,
        ILogger<SqlServerMemory>? log = null)
    {
        this._embeddingGenerator = embeddingGenerator;
        this._log = log ?? DefaultLogger<SqlServerMemory>.Instance;

        this._config = config;

        if (this._embeddingGenerator == null)
        {
            throw new SqlServerMemoryException("Embedding generator not configured");
        }

        this.CreateTablesIfNotExists();
    }

    /// <inheritdoc/>
    public async Task CreateIndexAsync(string index, int vectorSize, CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);

        if (await this.DoesIndexExistsAsync(index, cancellationToken).ConfigureAwait(false))
        {
            // Index already exists
            return;
        }

        using var connection = new SqlConnection(this._config.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using (SqlCommand command = connection.CreateCommand())
        {
            command.CommandText = $@"
                    BEGIN TRANSACTION;

                    INSERT INTO {this.GetFullTableName(this._config.MemoryCollectionTableName)}([id])
                    VALUES (@index);

                    IF OBJECT_ID(N'{this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}")}', N'U') IS NULL
                    CREATE TABLE {this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}")}
                    (
                        [memory_id] UNIQUEIDENTIFIER NOT NULL,
                        [vector_value_id] [int] NOT NULL,
                        [vector_value] [float] NOT NULL
                        FOREIGN KEY ([memory_id]) REFERENCES {this.GetFullTableName(this._config.MemoryTableName)}([id])
                    );

                    IF OBJECT_ID(N'[{this._config.Schema}.IXC_{$"{this._config.EmbeddingsTableName}_{index}"}]', N'U') IS NULL
                    CREATE CLUSTERED COLUMNSTORE INDEX [IXC_{$"{this._config.EmbeddingsTableName}_{index}"}]
                    ON {this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}")}
                    {(this.GetSqlServerMajorVersionNumber() >= 16 ? "ORDER ([memory_id])" : "")};

                    IF OBJECT_ID(N'{this.GetFullTableName($"{this._config.TagsTableName}_{index}")}', N'U') IS NULL
                    CREATE TABLE {this.GetFullTableName($"{this._config.TagsTableName}_{index}")}
                    (
                        [memory_id] UNIQUEIDENTIFIER NOT NULL,
                        [name] NVARCHAR(256)  NOT NULL,
                        [value] NVARCHAR(256) NOT NULL,
                        FOREIGN KEY ([memory_id]) REFERENCES {this.GetFullTableName(this._config.MemoryTableName)}([id])
                    );

                    COMMIT;
                ";

            command.Parameters.AddWithValue("@index", index);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);

        if (!(await this.DoesIndexExistsAsync(index, cancellationToken).ConfigureAwait(false)))
        {
            // Index does not exist
            return;
        }

        using var connection = new SqlConnection(this._config.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using (SqlCommand command = connection.CreateCommand())
        {
            command.CommandText = $@"
            BEGIN TRANSACTION;

            DELETE [embeddings]
            FROM {this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}")} [embeddings]
            INNER JOIN {this.GetFullTableName(this._config.MemoryTableName)} ON [embeddings].[memory_id] = {this.GetFullTableName(this._config.MemoryTableName)}.[id]
            WHERE
                {this.GetFullTableName(this._config.MemoryTableName)}.[collection] = @index
            AND {this.GetFullTableName(this._config.MemoryTableName)}.[key]=@key;

            DELETE [tags]
            FROM {this.GetFullTableName($"{this._config.TagsTableName}_{index}")} [tags]
            INNER JOIN {this.GetFullTableName(this._config.MemoryTableName)} ON [tags].[memory_id] = {this.GetFullTableName(this._config.MemoryTableName)}.[id]
            WHERE
                {this.GetFullTableName(this._config.MemoryTableName)}.[collection] = @index
            AND {this.GetFullTableName(this._config.MemoryTableName)}.[key]=@key;

            DELETE FROM {this.GetFullTableName(this._config.MemoryTableName)} WHERE [collection] = @index AND [key]=@key;

            COMMIT;";

            command.Parameters.AddWithValue("@index", index);
            command.Parameters.AddWithValue("@key", record.Id);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteIndexAsync(string index, CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);

        if (!(await this.DoesIndexExistsAsync(index, cancellationToken).ConfigureAwait(false)))
        {
            // Index does not exist
            return;
        }

        using var connection = new SqlConnection(this._config.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using (SqlCommand command = connection.CreateCommand())
        {
            command.CommandText = $@"
                    BEGIN TRANSACTION;

                    DROP TABLE {this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}")};
                    DROP TABLE {this.GetFullTableName($"{this._config.TagsTableName}_{index}")};

                    DELETE FROM {this.GetFullTableName(this._config.MemoryCollectionTableName)}
                                     WHERE [id] = @index;

                    COMMIT;";

            command.Parameters.AddWithValue("@index", index);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetIndexesAsync(CancellationToken cancellationToken = default)
    {
        List<string> indexes = new();

        using var connection = new SqlConnection(this._config.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using (SqlCommand command = connection.CreateCommand())
        {
            command.CommandText = $"SELECT [id] FROM {this.GetFullTableName(this._config.MemoryCollectionTableName)}";

            using var dataReader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                indexes.Add(dataReader.GetString(dataReader.GetOrdinal("id")));
            }
        }

        return indexes;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<MemoryRecord> GetListAsync(string index, ICollection<MemoryFilter>? filters = null, int limit = 1, bool withEmbeddings = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);

        if (!(await this.DoesIndexExistsAsync(index, cancellationToken).ConfigureAwait(false)))
        {
            // Index does not exist
            yield break;
        }

        string queryColumns = "[key], [payload], [tags]";

        if (withEmbeddings)
        {
            queryColumns += ", [embedding]";
        }

        if (limit < 0)
        {
            limit = int.MaxValue;
        }

        using var connection = new SqlConnection(this._config.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using (SqlCommand command = connection.CreateCommand())
        {
            var tagFilters = new TagCollection();

            command.CommandText = $@"
            WITH [filters] AS
		    (
			    SELECT
				    cast([filters].[key] AS NVARCHAR(256)) COLLATE SQL_Latin1_General_CP1_CI_AS AS [name],
				    cast([filters].[value] AS NVARCHAR(256)) COLLATE SQL_Latin1_General_CP1_CI_AS AS [value]
			    FROM openjson(@filters) [filters]
		    )
            SELECT TOP (@limit)
                {queryColumns}
            FROM
                {this.GetFullTableName(this._config.MemoryTableName)}
		    WHERE 1=1
            AND {this.GetFullTableName(this._config.MemoryTableName)}.[collection] = @index
            {this.GenerateFilters(index, command.Parameters, filters)};";

            command.Parameters.AddWithValue("@index", index);
            command.Parameters.AddWithValue("@limit", limit);
            command.Parameters.AddWithValue("@filters", JsonSerializer.Serialize(tagFilters));

            using var dataReader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return await this.ReadEntryAsync(dataReader, withEmbeddings, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<(MemoryRecord, double)> GetSimilarListAsync(string index, string text, ICollection<MemoryFilter>? filters = null, double minRelevance = 0, int limit = 1, bool withEmbeddings = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);

        if (!(await this.DoesIndexExistsAsync(index, cancellationToken).ConfigureAwait(false)))
        {
            // Index does not exist
            yield break;
        }

        Embedding embedding = await this._embeddingGenerator.GenerateEmbeddingAsync(text, cancellationToken).ConfigureAwait(false);

        string queryColumns = $"{this.GetFullTableName(this._config.MemoryTableName)}.[id]," +
                              $"{this.GetFullTableName(this._config.MemoryTableName)}.[key]," +
                              $"{this.GetFullTableName(this._config.MemoryTableName)}.[payload]," +
                              $"{this.GetFullTableName(this._config.MemoryTableName)}.[tags]";

        if (withEmbeddings)
        {
            queryColumns += $"," +
                            $"{this.GetFullTableName(this._config.MemoryTableName)}.[embedding]";
        }

        using var connection = new SqlConnection(this._config.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using (SqlCommand command = connection.CreateCommand())
        {
            var generatedFilters = this.GenerateFilters(index, command.Parameters, filters);

            command.CommandText = $@"
        WITH
        [embedding] as
        (
            SELECT
                cast([key] AS INT) AS [vector_value_id],
                cast([value] AS FLOAT) AS [vector_value]
            FROM
                openjson(@vector)
        ),
        [similarity] AS
        (
            SELECT TOP (@limit)
            {this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}")}.[memory_id],
            SUM([embedding].[vector_value] * {this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}")}.[vector_value]) /
            (
                SQRT(SUM([embedding].[vector_value] * [embedding].[vector_value]))
                *
                SQRT(SUM({this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}")}.[vector_value] * {this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}")}.[vector_value]))
            ) AS cosine_similarity
            -- sum([embedding].[vector_value] * {this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}")}.[vector_value]) as cosine_distance -- Optimized as per https://platform.openai.com/docs/guides/embeddings/which-distance-function-should-i-use
        FROM
            [embedding]
        INNER JOIN
            {this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}")} ON [embedding].vector_value_id = {this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}")}.vector_value_id
        INNER JOIN
            {this.GetFullTableName(this._config.MemoryTableName)} ON {this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}")}.[memory_id] = {this.GetFullTableName(this._config.MemoryTableName)}.[id]
        WHERE 1=1
        {generatedFilters}
        GROUP BY
            {this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}")}.[memory_id]
        ORDER BY
            cosine_similarity DESC
        )
        SELECT DISTINCT
            {queryColumns},
            [similarity].[cosine_similarity]
        FROM
            [similarity]
        INNER JOIN
            {this.GetFullTableName(this._config.MemoryTableName)} ON [similarity].[memory_id] = {this.GetFullTableName(this._config.MemoryTableName)}.[id]
        WHERE 1=1
        AND [cosine_similarity] >= @min_relevance_score
        {generatedFilters}
        ORDER BY [cosine_similarity] desc";

            command.Parameters.AddWithValue("@vector", JsonSerializer.Serialize(embedding.Data.ToArray()));
            command.Parameters.AddWithValue("@index", index);
            command.Parameters.AddWithValue("@min_relevance_score", minRelevance);
            command.Parameters.AddWithValue("@limit", limit);

            using var dataReader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                double cosineSimilarity = dataReader.GetDouble(dataReader.GetOrdinal("cosine_similarity"));
                yield return (await this.ReadEntryAsync(dataReader, withEmbeddings, cancellationToken).ConfigureAwait(false), cosineSimilarity);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<string> UpsertAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        await foreach (var item in this.BatchUpsertAsync(index, new[] { record }, cancellationToken).ConfigureAwait(false))
        {
            return item;
        }

        return null!;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> BatchUpsertAsync(string index, IEnumerable<MemoryRecord> records, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);

        if (!(await this.DoesIndexExistsAsync(index, cancellationToken).ConfigureAwait(false)))
        {
            throw new IndexNotFoundException($"The index '{index}' does not exist.");
        }

        using var connection = new SqlConnection(this._config.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        foreach (var record in records)
        {
            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText = $@"
                    BEGIN TRANSACTION;

                    MERGE INTO {this.GetFullTableName(this._config.MemoryTableName)}
                    USING (SELECT @key) as [src]([key])
                    ON {this.GetFullTableName(this._config.MemoryTableName)}.[key] = [src].[key]
                    WHEN MATCHED THEN
                        UPDATE SET payload=@payload, embedding=@embedding, tags=@tags
                    WHEN NOT MATCHED THEN
                        INSERT ([id], [key], [collection], [payload], [tags], [embedding])
                        VALUES (NEWID(), @key, @index, @payload, @tags, @embedding);

                    MERGE {this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}")} AS [tgt]
                    USING (
                        SELECT
                            {this.GetFullTableName(this._config.MemoryTableName)}.[id],
                            cast([vector].[key] AS INT) AS [vector_value_id],
                            cast([vector].[value] AS FLOAT) AS [vector_value]
                        FROM {this.GetFullTableName(this._config.MemoryTableName)}
                        CROSS APPLY
                            openjson(@embedding) [vector]
                        WHERE {this.GetFullTableName(this._config.MemoryTableName)}.[key] = @key
                            AND {this.GetFullTableName(this._config.MemoryTableName)}.[collection] = @index
                    ) AS [src]
                    ON [tgt].[memory_id] = [src].[id] AND [tgt].[vector_value_id] = [src].[vector_value_id]
                    WHEN MATCHED THEN
                        UPDATE SET [tgt].[vector_value] = [src].[vector_value]
                    WHEN NOT MATCHED THEN
                        INSERT ([memory_id], [vector_value_id], [vector_value])
                        VALUES ([src].[id],
                                [src].[vector_value_id],
                                [src].[vector_value] );

                    DELETE FROM [tgt]
                    FROM  {this.GetFullTableName($"{this._config.TagsTableName}_{index}")} AS [tgt]
                    INNER JOIN {this.GetFullTableName(this._config.MemoryTableName)} ON [tgt].[memory_id] = {this.GetFullTableName(this._config.MemoryTableName)}.[id]
                    WHERE {this.GetFullTableName(this._config.MemoryTableName)}.[key] = @key
                            AND {this.GetFullTableName(this._config.MemoryTableName)}.[collection] = @index;

                    MERGE {this.GetFullTableName($"{this._config.TagsTableName}_{index}")} AS [tgt]
                    USING (
                        SELECT
                            {this.GetFullTableName(this._config.MemoryTableName)}.[id],
                            cast([tags].[key] AS NVARCHAR(256)) COLLATE SQL_Latin1_General_CP1_CI_AS AS [tag_name],
                            [tag_value].[value] AS [value]
                        FROM {this.GetFullTableName(this._config.MemoryTableName)}
                        CROSS APPLY openjson(@tags) [tags]
                        CROSS APPLY openjson(cast([tags].[value] AS NVARCHAR(256)) COLLATE SQL_Latin1_General_CP1_CI_AS) [tag_value]
                        WHERE {this.GetFullTableName(this._config.MemoryTableName)}.[key] = @key
                            AND {this.GetFullTableName(this._config.MemoryTableName)}.[collection] = @index
                    ) AS [src]
                    ON [tgt].[memory_id] = [src].[id] AND [tgt].[name] = [src].[tag_name]
                    WHEN MATCHED THEN
                        UPDATE SET [tgt].[value] = [src].[value]
                    WHEN NOT MATCHED THEN
                        INSERT ([memory_id], [name], [value])
                        VALUES ([src].[id],
                                [src].[tag_name],
                                [src].[value]);

                    COMMIT;";

                command.Parameters.AddWithValue("@index", index);
                command.Parameters.AddWithValue("@key", record.Id);
                command.Parameters.AddWithValue("@payload", JsonSerializer.Serialize(record.Payload) ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@tags", JsonSerializer.Serialize(record.Tags) ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@embedding", JsonSerializer.Serialize(record.Vector.Data.ToArray()));

                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                yield return record.Id;
            }
        }
    }

    #region private ================================================================================

    /// <summary>
    /// Creates the SQL Server tables if they do not exist.
    /// </summary>
    /// <returns></returns>
    private void CreateTablesIfNotExists()
    {
        var sql = $@"IF NOT EXISTS (SELECT  *
                                    FROM    sys.schemas
                                    WHERE   name = N'{this._config.Schema}' )
                    EXEC('CREATE SCHEMA [{this._config.Schema}]');
                    IF OBJECT_ID(N'{this.GetFullTableName(this._config.MemoryCollectionTableName)}', N'U') IS NULL
                    CREATE TABLE {this.GetFullTableName(this._config.MemoryCollectionTableName)}
                    (   [id] NVARCHAR(256) NOT NULL,
                        PRIMARY KEY ([id])
                    );

                    IF OBJECT_ID(N'{this.GetFullTableName(this._config.MemoryTableName)}', N'U') IS NULL
                    CREATE TABLE {this.GetFullTableName(this._config.MemoryTableName)}
                    (   [id] UNIQUEIDENTIFIER NOT NULL,
                        [key] NVARCHAR(256)  NOT NULL,
                        [collection] NVARCHAR(256) NOT NULL,
                        [payload] NVARCHAR(MAX),
                        [tags] NVARCHAR(MAX),
                        [embedding] NVARCHAR(MAX),
                        PRIMARY KEY ([id]),
                        FOREIGN KEY ([collection]) REFERENCES {this.GetFullTableName(this._config.MemoryCollectionTableName)}([id]) ON DELETE CASCADE,
                        CONSTRAINT UK_{this._config.MemoryTableName} UNIQUE([collection], [key])
                    );
                    ";

        using var connection = new SqlConnection(this._config.ConnectionString);
        connection.Open();

        using (SqlCommand command = connection.CreateCommand())
        {
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Checks if the index exists.
    /// </summary>
    /// <param name="indexName">The index name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    private async Task<bool> DoesIndexExistsAsync(string indexName,
        CancellationToken cancellationToken = default)
    {
        var collections = await this.GetIndexesAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var item in collections)
        {
            if (item.Equals(indexName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the full table name with schema.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <returns></returns>
    private string GetFullTableName(string tableName)
    {
        return $"[{this._config.Schema}].[{tableName}]";
    }

    /// <summary>
    /// Generates the filters as SQL commands and sets the SQL parameters
    /// </summary>
    /// <param name="index">The index name.</param>
    /// <param name="parameters">The SQL parameters to populate.</param>
    /// <param name="filters">The filters to apply</param>
    /// <returns></returns>
    private string GenerateFilters(string index, SqlParameterCollection parameters, ICollection<MemoryFilter>? filters = null)
    {
        var filterBuilder = new StringBuilder();

        if (filters is null || filters.Count <= 0 || filters.All(f => f.Count <= 0))
        {
            return string.Empty;
        }

        filterBuilder.Append("AND ( ");

        for (int i = 0; i < filters.Count; i++)
        {
            var filter = filters.ElementAt(i);

            if (i > 0)
            {
                filterBuilder.Append(" OR ");
            }

            for (int j = 0; j < filter.Pairs.Count(); j++)
            {
                var value = filter.Pairs.ElementAt(j);

                if (j > 0)
                {
                    filterBuilder.Append(" AND ");
                }

                filterBuilder.Append(" ( ");

                filterBuilder.Append(CultureInfo.CurrentCulture, $@"EXISTS (
                         SELECT
	                        1
                        FROM {this.GetFullTableName($"{this._config.TagsTableName}_{index}")} AS [tags]
                        WHERE
	                        [tags].[memory_id] = {this.GetFullTableName(this._config.MemoryTableName)}.[id]
                            AND [name] = @filter_{i}_{j}_name
                            AND [value] = @filter_{i}_{j}_value
                        )
                    ");

                filterBuilder.Append(" ) ");

                parameters.AddWithValue($"@filter_{i}_{j}_name", value.Key);
                parameters.AddWithValue($"@filter_{i}_{j}_value", value.Value);
            }
        }

        filterBuilder.Append(" )");

        return filterBuilder.ToString();
    }

    private async Task<MemoryRecord> ReadEntryAsync(SqlDataReader dataReader, bool withEmbedding, CancellationToken cancellationToken = default)
    {
        var entry = new MemoryRecord();

        entry.Id = dataReader.GetString(dataReader.GetOrdinal("key"));

        if (!(await dataReader.IsDBNullAsync(dataReader.GetOrdinal("payload"), cancellationToken).ConfigureAwait(false)))
        {
            entry.Payload = JsonSerializer.Deserialize<Dictionary<string, object>>(dataReader.GetString(dataReader.GetOrdinal("payload")))!;
        }

        if (!(await dataReader.IsDBNullAsync(dataReader.GetOrdinal("tags"), cancellationToken).ConfigureAwait(false)))
        {
            entry.Tags = JsonSerializer.Deserialize<TagCollection>(dataReader.GetString(dataReader.GetOrdinal("tags")))!;
        }

        if (withEmbedding)
        {
            entry.Vector = new ReadOnlyMemory<float>(JsonSerializer.Deserialize<IEnumerable<float>>(dataReader.GetString(dataReader.GetOrdinal("embedding")))!.ToArray());
        }

        return entry;
    }

    // Note: "_" is allowed in Postgres, but we normalize it to "-" for consistency with other DBs
    private static readonly Regex s_replaceIndexNameCharsRegex = new(@"[\s|\\|/|.|_|:]");
    private const string ValidSeparator = "-";

    private static string NormalizeIndexName(string index)
    {
        ArgumentNullExceptionEx.ThrowIfNullOrWhiteSpace(index, nameof(index), "The index name is empty");

        index = s_replaceIndexNameCharsRegex.Replace(index.Trim().ToLowerInvariant(), ValidSeparator);

        return index;
    }

    private int GetSqlServerMajorVersionNumber()
    {
        using var connection = new SqlConnection(this._config.ConnectionString);
        connection.Open();

        using (SqlCommand command = connection.CreateCommand())
        {
            command.CommandText = "SELECT SERVERPROPERTY('ProductMajorVersion')";

            var result = command.ExecuteScalar();

            return Convert.ToInt32(result, CultureInfo.InvariantCulture);
        }
    }

    #endregion
}
