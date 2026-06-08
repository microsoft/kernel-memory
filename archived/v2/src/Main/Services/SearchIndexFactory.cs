// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Config.SearchIndex;
using KernelMemory.Core.Embeddings.Cache;
using KernelMemory.Core.Search;
using Microsoft.Extensions.Logging;

namespace KernelMemory.Main.Services;

/// <summary>
/// Factory for creating search indexes from configuration.
/// </summary>
public static class SearchIndexFactory
{
    /// <summary>
    /// Creates all search indexes from configuration (FTS, vector, and future types).
    /// </summary>
    /// <param name="configs">List of search index configurations.</param>
    /// <param name="httpClient">HTTP client for embedding API calls (required for vector indexes).</param>
    /// <param name="embeddingCache">Optional embedding cache for vector indexes.</param>
    /// <param name="loggerFactory">Logger factory for creating component loggers.</param>
    /// <returns>Dictionary of index ID to ISearchIndex instance.</returns>
    public static IReadOnlyDictionary<string, ISearchIndex> CreateIndexes(
        List<SearchIndexConfig> configs,
        HttpClient httpClient,
        IEmbeddingCache? embeddingCache,
        ILoggerFactory loggerFactory)
    {
        var indexes = new Dictionary<string, ISearchIndex>();

        foreach (var config in configs)
        {
            if (config is FtsSearchIndexConfig ftsConfig)
            {
                var ftsIndex = CreateFtsIndexFromConfig(ftsConfig, loggerFactory);
                indexes[config.Id] = ftsIndex;
            }
            else if (config is VectorSearchIndexConfig vectorConfig)
            {
                var vectorIndex = CreateVectorIndexFromConfig(vectorConfig, httpClient, embeddingCache, loggerFactory);
                indexes[config.Id] = vectorIndex;
            }
            // Add other index types here (graph, hybrid, etc.)
        }

        return indexes;
    }

    /// <summary>
    /// Creates an FTS index from configuration.
    /// </summary>
    private static SqliteFtsIndex CreateFtsIndexFromConfig(
        FtsSearchIndexConfig config,
        ILoggerFactory loggerFactory)
    {
        if (string.IsNullOrWhiteSpace(config.Path))
        {
            throw new InvalidOperationException($"FTS index '{config.Id}' has no Path configured");
        }

        var logger = loggerFactory.CreateLogger<SqliteFtsIndex>();
        return new SqliteFtsIndex(config.Path, config.EnableStemming, logger);
    }

    /// <summary>
    /// Creates a vector index from configuration.
    /// Requires embeddings configuration to be present.
    /// </summary>
    private static SqliteVectorIndex CreateVectorIndexFromConfig(
        VectorSearchIndexConfig config,
        HttpClient httpClient,
        IEmbeddingCache? embeddingCache,
        ILoggerFactory loggerFactory)
    {
        if (string.IsNullOrWhiteSpace(config.Path))
        {
            throw new InvalidOperationException($"Vector index '{config.Id}' has no Path configured");
        }

        if (config.Embeddings == null)
        {
            throw new InvalidOperationException($"Vector index '{config.Id}' has no Embeddings configuration");
        }

        // Create embedding generator from config
        var embeddingGenerator = EmbeddingGeneratorFactory.CreateGenerator(
            config.Embeddings,
            httpClient,
            embeddingCache,
            loggerFactory);

        var logger = loggerFactory.CreateLogger<SqliteVectorIndex>();

        return new SqliteVectorIndex(
            config.Path,
            config.Dimensions,
            config.UseSqliteVec,
            embeddingGenerator,
            logger);
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
