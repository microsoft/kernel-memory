// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Config.SearchIndex;
using KernelMemory.Core.Search;
using Microsoft.Extensions.Logging;

namespace KernelMemory.Main.Services;

/// <summary>
/// Factory for creating search index instances from configuration.
/// </summary>
public static class SearchIndexFactory
{
    /// <summary>
    /// Creates search index instances from node configuration.
    /// </summary>
    /// <param name="searchIndexConfigs">Search index configurations from node.</param>
    /// <param name="loggerFactory">Logger factory for creating loggers.</param>
    /// <returns>Dictionary mapping index ID to ISearchIndex instance.</returns>
    public static IReadOnlyDictionary<string, ISearchIndex> CreateIndexes(
        IReadOnlyList<SearchIndexConfig> searchIndexConfigs,
        ILoggerFactory loggerFactory)
    {
        var indexes = new Dictionary<string, ISearchIndex>();

        foreach (var config in searchIndexConfigs)
        {
            var index = CreateIndex(config, loggerFactory);
            if (index != null)
            {
                indexes[config.Id] = index;
            }
        }

        return indexes;
    }

    /// <summary>
    /// Creates a single search index instance from configuration.
    /// </summary>
    /// <param name="config">Search index configuration.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <returns>ISearchIndex instance, or null if type not supported.</returns>
    private static SqliteFtsIndex? CreateIndex(SearchIndexConfig config, ILoggerFactory loggerFactory)
    {
        return config switch
        {
            FtsSearchIndexConfig ftsConfig when !string.IsNullOrWhiteSpace(ftsConfig.Path) =>
                new SqliteFtsIndex(
                    ftsConfig.Path,
                    ftsConfig.EnableStemming,
                    loggerFactory.CreateLogger<SqliteFtsIndex>()),

            // Vector and Graph indexes not yet implemented
            // VectorSearchIndexConfig vectorConfig => ...,
            // GraphSearchIndexConfig graphConfig => ...,

            _ => null
        };
    }
}
