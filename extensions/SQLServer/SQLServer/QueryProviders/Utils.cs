// Copyright (c) Microsoft. All rights reserved.

using System.Globalization;
using System.Text;
using Microsoft.Data.SqlClient;

namespace Microsoft.KernelMemory.MemoryDb.SQLServer.QueryProviders;

internal static class Utils
{
    /// <summary>
    /// Gets the full table name with schema.
    /// </summary>
    /// <param name="config">Server settings</param>
    /// <param name="tableName">The table name.</param>
    internal static string GetFullTableName(SqlServerConfig config, string tableName)
    {
        return $"[{config.Schema}].[{tableName}]";
    }

    /// <summary>
    /// Generates the filters as SQL commands and sets the SQL parameters
    /// </summary>
    /// <param name="config">Server settings</param>
    /// <param name="index">The index name.</param>
    /// <param name="parameters">The SQL parameters to populate.</param>
    /// <param name="filters">The filters to apply</param>
    internal static string GenerateFilters(
        SqlServerConfig config,
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
                        FROM {GetFullTableName(config, $"{config.TagsTableName}_{index}")} AS [tags]
                        WHERE
	                        [tags].[memory_id] = {GetFullTableName(config, config.MemoryTableName)}.[id]
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
