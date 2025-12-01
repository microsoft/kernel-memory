// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Config.SearchIndex;
using KernelMemory.Core.Search;
using Microsoft.Extensions.Logging;

namespace KernelMemory.Main.Services;

/// <summary>
/// Factory for creating search indexes from configuration.
/// </summary>
public static class SearchIndexFactory
{
    /// <summary>
    /// Creates search indexes from configuration as a dictionary keyed by index ID.
    /// </summary>
    /// <param name="configs">List of search index configurations.</param>
    /// <param name="loggerFactory">Logger factory for creating index loggers.</param>
    /// <returns>Dictionary of index ID to ISearchIndex instance.</returns>
    public static IReadOnlyDictionary<string, ISearchIndex> CreateIndexes(
        List<SearchIndexConfig> configs,
        ILoggerFactory loggerFactory)
    {
        var indexes = new Dictionary<string, ISearchIndex>();

        foreach (var config in configs)
        {
            if (config is FtsSearchIndexConfig ftsConfig)
            {
                if (string.IsNullOrWhiteSpace(ftsConfig.Path))
                {
                    throw new InvalidOperationException($"FTS index '{config.Id}' has no Path configured");
                }

                var logger = loggerFactory.CreateLogger<SqliteFtsIndex>();
                var index = new SqliteFtsIndex(ftsConfig.Path, ftsConfig.EnableStemming, logger);
                indexes[config.Id] = index;
            }
            // Add other index types here (vector, hybrid, etc.)
        }

        return indexes;
    }

    /// <summary>
    /// Creates the first FTS index from configuration.
    /// Returns null if no FTS index is configured.
    /// </summary>
    /// <param name="configs">List of search index configurations.</param>
    /// <returns>The first FTS index, or null if none configured.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "LoggerFactory lifetime is managed by the logger infrastructure. Short-lived CLI commands don't require explicit disposal.")]
    public static IFtsIndex? CreateFtsIndex(List<SearchIndexConfig> configs)
    {
        foreach (var config in configs)
        {
            if (config is FtsSearchIndexConfig ftsConfig)
            {
                if (string.IsNullOrWhiteSpace(ftsConfig.Path))
                {
                    throw new InvalidOperationException($"FTS index '{config.Id}' has no Path configured");
                }

                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Debug);
                });
                var logger = loggerFactory.CreateLogger<SqliteFtsIndex>();
                return new SqliteFtsIndex(ftsConfig.Path, ftsConfig.EnableStemming, logger);
            }
        }

        return null;
    }
}
