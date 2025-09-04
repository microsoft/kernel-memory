// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Data.SqlClient;

namespace Microsoft.KernelMemory.MemoryDb.SQLServer.QueryProviders;

internal sealed class VectorQueryProvider : ISqlServerQueryProvider
{
    private readonly SqlServerConfig _config;

    public VectorQueryProvider(SqlServerConfig config)
    {
        this._config = config;
    }

    /// <inheritdoc/>
    public string PrepareCreateIndexQuery(int sqlServerVersion, string index, int vectorSize)
    {
        // Cache table names for readability
        var collectionsTable = this.GetFullTableName(this._config.MemoryCollectionTableName);
        var memoryTable = this.GetFullTableName(this._config.MemoryTableName);
        var tagsIndexTable = this.GetFullTableName($"{this._config.TagsTableName}_{index}");

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

                   COMMIT;
                   """;

        return sql;
    }

    /// <inheritdoc/>
    public string PrepareDeleteRecordQuery(string index)
    {
        var memoryTable = this.GetFullTableName(this._config.MemoryTableName);
        var tagsIndexTable = this.GetFullTableName($"{this._config.TagsTableName}_{index}");

        var sql = $"""
                   BEGIN TRANSACTION;

                       DELETE [tags]
                           FROM {tagsIndexTable} [tags]
                           INNER JOIN {memoryTable} ON [tags].[memory_id] = {memoryTable}.[id]
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
        var tagsIndexTable = this.GetFullTableName($"{this._config.TagsTableName}_{index}");
        var collectionsTable = this.GetFullTableName(this._config.MemoryCollectionTableName);

        var sql = $"""
                   BEGIN TRANSACTION;

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
        if (withEmbeddings) { queryColumns += ", CAST([embedding] AS NVARCHAR(MAX)) AS [embedding]"; }

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
        var vectorSize = this._config.VectorSize;

        var queryColumns = $"{memoryTable}.[id]," +
                           $"{memoryTable}.[key]," +
                           $"{memoryTable}.[payload]," +
                           $"{memoryTable}.[tags]";

        if (withEmbedding)
        {
            queryColumns += $",CAST({memoryTable}.[embedding] AS NVARCHAR(MAX)) AS [embedding]";
        }

        var generatedFilters = this.GenerateFilters(index, parameters, filters);

        var sql = $"""
                   SELECT TOP (@limit)
                       {queryColumns},
                       VECTOR_DISTANCE('cosine', CAST(@vector AS VECTOR({vectorSize})), Embedding) AS [distance]
                   FROM
                       {memoryTable}
                   WHERE
                       VECTOR_DISTANCE('cosine', CAST(@vector AS VECTOR({vectorSize})), Embedding) <= @max_distance
                       {generatedFilters}
                   ORDER BY [distance] ASC
                   """;

        return sql;
    }

    /// <inheritdoc/>
    public string PrepareUpsertRecordsBatchQuery(string index)
    {
        var memoryTable = this.GetFullTableName(this._config.MemoryTableName);
        var tagsIndexTable = this.GetFullTableName($"{this._config.TagsTableName}_{index}");
        var vectorSize = this._config.VectorSize;

        var sql = $"""
                   BEGIN TRANSACTION;

                       MERGE INTO {memoryTable}
                           USING (SELECT @key) as [src]([key])
                           ON {memoryTable}.[key] = [src].[key]
                           WHEN MATCHED THEN
                               UPDATE SET payload=@payload, embedding=CAST(@embedding AS VECTOR({vectorSize})), tags=@tags
                           WHEN NOT MATCHED THEN
                               INSERT ([key], [collection], [payload], [tags], [embedding])
                               VALUES (@key, @index, @payload, @tags, CAST(@embedding AS VECTOR({vectorSize})));

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
        var vectorSize = this._config.VectorSize;
        var schema = this._config.Schema;
        var memoryTableName = this._config.MemoryTableName; // Used inside constraint name

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
                       (   [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                           [key] NVARCHAR(256)  NOT NULL,
                           [collection] NVARCHAR(256) NOT NULL,
                           [payload] NVARCHAR(MAX),
                           [tags] NVARCHAR(MAX),
                           [embedding] VECTOR({vectorSize}),
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
