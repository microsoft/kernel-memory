// Copyright (c) Microsoft. All rights reserved.

using System.Globalization;
using System.Text;
using Microsoft.Data.SqlClient;

namespace Microsoft.KernelMemory.MemoryDb.SQLServer.QueryProviders;

internal abstract class SqlServerQueryProvider
{
    protected readonly SqlServerConfig Config;

    protected SqlServerQueryProvider(SqlServerConfig config)
    {
        this.Config = config;
    }

    public abstract string GetCreateIndexQuery(int sqlServerVersion, string index, int vectorSize);

    public abstract string GetDeleteQuery(string index);

    public abstract string GetDeleteIndexQuery(string index);

    public abstract string GetIndexesQuery();

    public abstract string GetListQuery(string index,
        ICollection<MemoryFilter>? filters,
        bool withEmbedding,
        SqlParameterCollection parameters);

    public abstract string GetSimilarityListQuery(string index,
        ICollection<MemoryFilter>? filters,
        bool withEmbedding,
        SqlParameterCollection parameters);

    public abstract string GetUpsertBatchQuery(string index);

    public abstract string GetCreateTablesQuery();

    /// <summary>
    /// Gets the full table name with schema.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <returns></returns>
    protected string GetFullTableName(string tableName)
    {
        return $"[{this.Config.Schema}].[{tableName}]";
    }

    /// <summary>
    /// Generates the filters as SQL commands and sets the SQL parameters
    /// </summary>
    /// <param name="index">The index name.</param>
    /// <param name="parameters">The SQL parameters to populate.</param>
    /// <param name="filters">The filters to apply</param>
    protected string GenerateFilters(
        string index,
        SqlParameterCollection parameters,
        ICollection<MemoryFilter>? filters)
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
                        FROM {this.GetFullTableName($"{this.Config.TagsTableName}_{index}")} AS [tags]
                        WHERE
	                        [tags].[memory_id] = {this.GetFullTableName(this.Config.MemoryTableName)}.[id]
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
}
