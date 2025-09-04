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
        // Cache table names
        var collectionsTable = this.GetFullTableName(this._config.MemoryCollectionTableName);
        var memoryTable = this.GetFullTableName(this._config.MemoryTableName);
        var tagsIndexTable = this.GetFullTableName($"{this._config.TagsTableName}_{index}");
        var embeddingsIndexTable = this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}");
        var schema = this._config.Schema;
        var embeddingsIndexName = $"IXC_{this._config.EmbeddingsTableName}_{index}";

        var sql = $"""
                   BEGIN TRANSACTION;

                       INSERT INTO {collectionsTable}([id])
                           VALUES (@index);

                       IF OBJECT_ID(N'{tagsIndexTable}', N'U') IS NULL
                           CREATE TABLE {tagsIndexTable}
                           (
                               [memory_id] UNIQUEIDENTIFIER NOT NULL,
                               [name] NVARCHAR(256)  NOT NULL,
                               [value] NVARCHAR(256) NOT NULL,
                               FOREIGN KEY ([memory_id]) REFERENCES {memoryTable}([id])
                           );

                       IF OBJECT_ID(N'{embeddingsIndexTable}', N'U') IS NULL
                           CREATE TABLE {embeddingsIndexTable}
                           (
                               [memory_id] UNIQUEIDENTIFIER NOT NULL,
                               [vector_value_id] [int] NOT NULL,
                               [vector_value] [float] NOT NULL,
                               FOREIGN KEY ([memory_id]) REFERENCES {memoryTable}([id])
                           );

                       IF OBJECT_ID(N'[{schema}.{embeddingsIndexName}]', N'U') IS NULL
                           CREATE CLUSTERED COLUMNSTORE INDEX [{embeddingsIndexName}]
                           ON {embeddingsIndexTable}
                           {(sqlServerVersion >= 16 ? "ORDER ([memory_id])" : "")};

                   COMMIT;
                   """;

        return sql;
    }

    /// <inheritdoc/>
    public string PrepareDeleteRecordQuery(string index)
    {
        var memoryTable = this.GetFullTableName(this._config.MemoryTableName);
        var tagsIndexTable = this.GetFullTableName($"{this._config.TagsTableName}_{index}");
        var embeddingsIndexTable = this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}");

        var sql = $"""
                   BEGIN TRANSACTION;

                       DELETE [tags]
                           FROM {tagsIndexTable} [tags]
                           INNER JOIN {memoryTable} ON [tags].[memory_id] = {memoryTable}.[id]
                           WHERE
                               {memoryTable}.[collection] = @index
                           AND {memoryTable}.[key]=@key;

                       DELETE [embeddings]
                           FROM {embeddingsIndexTable} [embeddings]
                           INNER JOIN {memoryTable} ON [embeddings].[memory_id] = {memoryTable}.[id]
                           WHERE
                               {memoryTable}.[collection] = @index
                               AND {memoryTable}.[key]=@key;

                       DELETE FROM {memoryTable}
                              WHERE [collection] = @index AND [key]=@key;

                   COMMIT;
                   """;

        return sql;
    }

    /// <inheritdoc/>
    public string PrepareDeleteIndexQuery(string index)
    {
        var embeddingsIndexTable = this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}");
        var tagsIndexTable = this.GetFullTableName($"{this._config.TagsTableName}_{index}");
        var collectionsTable = this.GetFullTableName(this._config.MemoryCollectionTableName);

        var sql = $"""
                   BEGIN TRANSACTION;

                       DROP TABLE {embeddingsIndexTable};
                       DROP TABLE {tagsIndexTable};

                       DELETE FROM {collectionsTable}
                              WHERE [id] = @index;

                   COMMIT;
                   """;

        return sql;
    }

    /// <inheritdoc/>
    public string PrepareGetIndexesQuery()
    {
        var collectionsTable = this.GetFullTableName(this._config.MemoryCollectionTableName);
        var sql = $"SELECT [id] FROM {collectionsTable}";
        return sql;
    }

    /// <inheritdoc/>
    public string PrepareGetRecordsListQuery(string index,
        ICollection<MemoryFilter>? filters,
        bool withEmbeddings,
        SqlParameterCollection parameters)
    {
        var memoryTable = this.GetFullTableName(this._config.MemoryTableName);

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
                       {memoryTable}
                   WHERE
                       {memoryTable}.[collection] = @index
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
        var memoryTable = this.GetFullTableName(this._config.MemoryTableName);
        var embeddingsIndexTable = this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}");

        var queryColumns = $"{memoryTable}.[id]," +
                           $"{memoryTable}.[key]," +
                           $"{memoryTable}.[payload]," +
                           $"{memoryTable}.[tags]";

        if (withEmbedding)
        {
            queryColumns += $",{memoryTable}.[embedding]";
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
                           {embeddingsIndexTable}.[memory_id],
                           SUM([embedding].[vector_value] * {embeddingsIndexTable}.[vector_value]) /
                           (
                               SQRT(SUM([embedding].[vector_value] * [embedding].[vector_value]))
                               *
                               SQRT(SUM({embeddingsIndexTable}.[vector_value] * {embeddingsIndexTable}.[vector_value]))
                           ) AS cosine_similarity
                       FROM
                           [embedding]
                       INNER JOIN
                           {embeddingsIndexTable} ON [embedding].vector_value_id = {embeddingsIndexTable}.vector_value_id
                       INNER JOIN
                           {memoryTable} ON {embeddingsIndexTable}.[memory_id] = {memoryTable}.[id]
                       WHERE 1=1
                           {generatedFilters}
                       GROUP BY
                           {embeddingsIndexTable}.[memory_id]
                       ORDER BY
                           cosine_similarity DESC
                   )
                   SELECT DISTINCT
                       {queryColumns},
                       [similarity].[cosine_similarity]
                   FROM
                       [similarity]
                   INNER JOIN
                       {memoryTable} ON [similarity].[memory_id] = {memoryTable}.[id]
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
        var memoryTable = this.GetFullTableName(this._config.MemoryTableName);
        var embeddingsIndexTable = this.GetFullTableName($"{this._config.EmbeddingsTableName}_{index}");
        var tagsIndexTable = this.GetFullTableName($"{this._config.TagsTableName}_{index}");

        var sql = $"""
                   BEGIN TRANSACTION;

                       MERGE INTO {memoryTable}
                           USING (SELECT @key) as [src]([key])
                           ON {memoryTable}.[key] = [src].[key]
                           WHEN MATCHED THEN
                               UPDATE SET payload=@payload, embedding=@embedding, tags=@tags
                           WHEN NOT MATCHED THEN
                               INSERT ([id], [key], [collection], [payload], [tags], [embedding])
                               VALUES (NEWID(), @key, @index, @payload, @tags, @embedding);

                       MERGE {embeddingsIndexTable} AS [tgt]
                           USING (
                               SELECT
                                   {memoryTable}.[id],
                                   cast([vector].[key] AS INT) AS [vector_value_id],
                                   cast([vector].[value] AS FLOAT) AS [vector_value]
                               FROM {memoryTable}
                               CROSS APPLY
                                   openjson(@embedding) [vector]
                               WHERE {memoryTable}.[key] = @key
                                   AND {memoryTable}.[collection] = @index
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
                           FROM  {tagsIndexTable} AS [tgt]
                           INNER JOIN {memoryTable} ON [tgt].[memory_id] = {memoryTable}.[id]
                           WHERE {memoryTable}.[key] = @key
                                   AND {memoryTable}.[collection] = @index;

                       MERGE {tagsIndexTable} AS [tgt]
                           USING (
                               SELECT
                                   {memoryTable}.[id],
                                   cast([tags].[key] AS NVARCHAR(MAX)) COLLATE SQL_Latin1_General_CP1_CI_AS AS [tag_name],
                                   [tag_value].[value] AS [value]
                               FROM {memoryTable}
                               CROSS APPLY openjson(@tags) [tags]
                               CROSS APPLY openjson(cast([tags].[value] AS NVARCHAR(MAX)) COLLATE SQL_Latin1_General_CP1_CI_AS) [tag_value]
                               WHERE {memoryTable}.[key] = @key
                                   AND {memoryTable}.[collection] = @index
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
        var collectionsTable = this.GetFullTableName(this._config.MemoryCollectionTableName);
        var memoryTable = this.GetFullTableName(this._config.MemoryTableName);
        var schema = this._config.Schema;
        var memoryTableName = this._config.MemoryTableName; // used for constraint name

        var sql = $"""
                   IF NOT EXISTS (SELECT  *
                                   FROM   sys.schemas
                                   WHERE  name = N'{schema}' )
                   EXEC('CREATE SCHEMA [{schema}]');

                   IF OBJECT_ID(N'{collectionsTable}', N'U') IS NULL
                       CREATE TABLE {collectionsTable}
                       (   [id] NVARCHAR(256) NOT NULL,
                           PRIMARY KEY ([id])
                       );

                   IF OBJECT_ID(N'{memoryTable}', N'U') IS NULL
                       CREATE TABLE {memoryTable}
                       (   [id] UNIQUEIDENTIFIER NOT NULL,
                           [key] NVARCHAR(256)  NOT NULL,
                           [collection] NVARCHAR(256) NOT NULL,
                           [payload] NVARCHAR(MAX),
                           [tags] NVARCHAR(MAX),
                           [embedding] NVARCHAR(MAX),
                           PRIMARY KEY ([id]),
                           FOREIGN KEY ([collection]) REFERENCES {collectionsTable}([id]) ON DELETE CASCADE,
                           CONSTRAINT UK_{memoryTableName} UNIQUE([collection], [key])
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
