// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Data.SqlClient;

namespace Microsoft.KernelMemory.MemoryDb.SQLServer.QueryProviders;

internal sealed class DefaultQueryProvider : ISqlServerQueryProvider
{
    private readonly SqlServerConfig _config;

    public DefaultQueryProvider(SqlServerConfig config)
    {
        this._config = config;
    }

    /// <inheritdoc/>
    public string PrepareCreateIndexQuery(int sqlServerVersion, string index, int vectorSize)
    {
        var sql = $"""
                   BEGIN TRANSACTION;

                       INSERT INTO {this.GetFullTableName(this._config.MemoryCollectionTableName)}([id])
                           VALUES (@index);

                       IF OBJECT_ID(N'{this.GetFullTableName($"{this._config.TagsTableName}_{index}")}', N'U') IS NULL
                           CREATE TABLE {this.GetFullTableName($"{this._config.TagsTableName}_{index}")}
                           (
                               [memory_id] UNIQUEIDENTIFIER NOT NULL,
                               [name] NVARCHAR(256)  NOT NULL,
                               [value] NVARCHAR(256) NOT NULL,
                               FOREIGN KEY ([memory_id]) REFERENCES {this.GetFullTableName(this._config.MemoryTableName)}([id])
                           );

                       IF OBJECT_ID(N'{this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}")}', N'U') IS NULL
                           CREATE TABLE {this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}")}
                           (
                               [memory_id] UNIQUEIDENTIFIER NOT NULL,
                               [vector_value_id] [int] NOT NULL,
                               [vector_value] [float] NOT NULL,
                               FOREIGN KEY ([memory_id]) REFERENCES {this.GetFullTableName(this._config.MemoryTableName)}([id])
                           );

                       IF OBJECT_ID(N'[{this._config.Schema}.IXC_{$"{this._config.EmbeddingsTableName}_{index}"}]', N'U') IS NULL
                           CREATE CLUSTERED COLUMNSTORE INDEX [IXC_{$"{this._config.EmbeddingsTableName}_{index}"}]
                           ON {this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}")}
                           {(sqlServerVersion >= 16 ? "ORDER ([memory_id])" : "")};

                   COMMIT;
                   """;

        return sql;
    }

    /// <inheritdoc/>
    public string PrepareDeleteRecordQuery(string index)
    {
        var sql = $"""
                   BEGIN TRANSACTION;

                       DELETE [tags]
                           FROM {this.GetFullTableName($"{this._config.TagsTableName}_{index}")} [tags]
                           INNER JOIN {this.GetFullTableName(this._config.MemoryTableName)} ON [tags].[memory_id] = {this.GetFullTableName(this._config.MemoryTableName)}.[id]
                           WHERE
                               {this.GetFullTableName(this._config.MemoryTableName)}.[collection] = @index
                           AND {this.GetFullTableName(this._config.MemoryTableName)}.[key]=@key;

                       DELETE [embeddings]
                           FROM {this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}")} [embeddings]
                           INNER JOIN {this.GetFullTableName(this._config.MemoryTableName)} ON [embeddings].[memory_id] = {this.GetFullTableName(this._config.MemoryTableName)}.[id]
                           WHERE
                               {this.GetFullTableName(this._config.MemoryTableName)}.[collection] = @index
                               AND {this.GetFullTableName(this._config.MemoryTableName)}.[key]=@key;

                       DELETE FROM {this.GetFullTableName(this._config.MemoryTableName)}
                              WHERE [collection] = @index AND [key]=@key;

                   COMMIT;
                   """;

        return sql;
    }

    /// <inheritdoc/>
    public string PrepareDeleteIndexQuery(string index)
    {
        var sql = $"""
                   BEGIN TRANSACTION;

                       DROP TABLE {this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}")};
                       DROP TABLE {this.GetFullTableName($"{this._config.TagsTableName}_{index}")};

                       DELETE FROM {this.GetFullTableName(this._config.MemoryCollectionTableName)}
                              WHERE [id] = @index;

                   COMMIT;
                   """;

        return sql;
    }

    /// <inheritdoc/>
    public string PrepareGetIndexesQuery()
    {
        var sql = $"SELECT [id] FROM {this.GetFullTableName(this._config.MemoryCollectionTableName)}";
        return sql;
    }

    /// <inheritdoc/>
    public string PrepareGetRecordsListQuery(string index,
        ICollection<MemoryFilter>? filters,
        bool withEmbeddings,
        SqlParameterCollection parameters)
    {
        var queryColumns = "[key], [payload], [tags]";
        if (withEmbeddings) { queryColumns += ", [embedding]"; }

        var sql = $"""
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
                   WHERE
                       {this.GetFullTableName(this._config.MemoryTableName)}.[collection] = @index
                       {this.GenerateFilters(index, parameters, filters)};
                   """;

        return sql;
    }

    /// <inheritdoc/>
    public string PrepareGetSimilarRecordsListQuery(string index,
        ICollection<MemoryFilter>? filters,
        bool withEmbedding,
        SqlParameterCollection parameters)
    {
        var queryColumns = $"{this.GetFullTableName(this._config.MemoryTableName)}.[id]," +
                           $"{this.GetFullTableName(this._config.MemoryTableName)}.[key]," +
                           $"{this.GetFullTableName(this._config.MemoryTableName)}.[payload]," +
                           $"{this.GetFullTableName(this._config.MemoryTableName)}.[tags]";

        if (withEmbedding)
        {
            queryColumns += $"," +
                            $"{this.GetFullTableName(this._config.MemoryTableName)}.[embedding]";
        }

        var generatedFilters = this.GenerateFilters(index, parameters, filters);

        var sql = $"""
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
                   WHERE
                       [cosine_similarity] >= @min_relevance_score
                       {generatedFilters}
                   ORDER BY [cosine_similarity] desc
                   """;

        return sql;
    }

    /// <inheritdoc/>
    public string PrepareUpsertRecordsBatchQuery(string index)
    {
        var sql = $"""
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
                                   cast([tags].[key] AS NVARCHAR(MAX)) COLLATE SQL_Latin1_General_CP1_CI_AS AS [tag_name],
                                   [tag_value].[value] AS [value]
                               FROM {this.GetFullTableName(this._config.MemoryTableName)}
                               CROSS APPLY openjson(@tags) [tags]
                               CROSS APPLY openjson(cast([tags].[value] AS NVARCHAR(MAX)) COLLATE SQL_Latin1_General_CP1_CI_AS) [tag_value]
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

                   COMMIT;
                   """;

        return sql;
    }

    /// <inheritdoc/>
    public string PrepareCreateAllSupportingTablesQuery()
    {
        var sql = $"""
                   IF NOT EXISTS (SELECT  *
                                   FROM   sys.schemas
                                   WHERE  name = N'{this._config.Schema}' )
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
                   """;

        return sql;
    }

    private string GetFullTableName(string tableName)
    {
        return Utils.GetFullTableName(this._config, tableName);
    }

    private string GenerateFilters(
        string index,
        SqlParameterCollection parameters,
        ICollection<MemoryFilter>? filters)
    {
        return Utils.GenerateFilters(this._config, index, parameters, filters);
    }
}
