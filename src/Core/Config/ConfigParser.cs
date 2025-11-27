// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json;
using KernelMemory.Core.Config.Cache;
using KernelMemory.Core.Config.ContentIndex;
using KernelMemory.Core.Config.SearchIndex;
using KernelMemory.Core.Config.Storage;
using KernelMemory.Core.Config.Validation;

namespace KernelMemory.Core.Config;

/// <summary>
/// Parses configuration files and returns validated AppConfig instances
/// Supports JSON with comments and case-insensitive property names
/// </summary>
public static class ConfigParser
{
    /// <summary>
    /// JSON serializer options configured for config parsing
    /// - Case insensitive property names
    /// - Comments allowed
    /// - Trailing commas allowed
    /// </summary>
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Loads configuration from a file, or returns default config if file doesn't exist
    /// Performs tilde expansion on paths (~/ → home directory)
    /// </summary>
    /// <param name="filePath">Path to configuration file</param>
    /// <returns>Validated AppConfig instance</returns>
    /// <exception cref="ConfigException">Thrown when file exists but parsing or validation fails</exception>
    public static AppConfig LoadFromFile(string filePath)
    {
        // If file doesn't exist, return default configuration
        if (!File.Exists(filePath))
        {
            return AppConfig.CreateDefault();
        }

        try
        {
            // Read file content
            var json = File.ReadAllText(filePath);

            // Parse and validate
            var config = ParseFromString(json);

            // Expand tilde paths
            ExpandTildePaths(config);

            return config;
        }
        catch (ConfigException)
        {
            // Re-throw configuration exceptions as-is
            throw;
        }
        catch (JsonException ex)
        {
            throw new ConfigException(
                filePath,
                $"Failed to parse configuration file: {ex.Message}",
                ex);
        }
        catch (Exception ex)
        {
            throw new ConfigException(
                filePath,
                $"Error reading configuration file: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Parses configuration from a JSON string
    /// </summary>
    /// <param name="json">JSON configuration string</param>
    /// <returns>Validated AppConfig instance</returns>
    /// <exception cref="ConfigException">Thrown when parsing or validation fails</exception>
    public static AppConfig ParseFromString(string json)
    {
        try
        {
            var config = JsonSerializer.Deserialize<AppConfig>(json, s_jsonOptions);

            if (config == null)
            {
                throw new ConfigException(
                    "root",
                    "Failed to deserialize configuration: result was null");
            }

            // Validate the configuration
            config.Validate();

            return config;
        }
        catch (ConfigException)
        {
            // Re-throw configuration exceptions as-is
            throw;
        }
        catch (JsonException ex)
        {
            throw new ConfigException(
                "root",
                $"Failed to parse configuration: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Expands tilde (~) in paths to the user's home directory
    /// Recursively processes all path properties in the configuration
    /// </summary>
    /// <param name="config"></param>
    private static void ExpandTildePaths(AppConfig config)
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        foreach (var (_, nodeConfig) in config.Nodes)
        {
            // Expand paths in ContentIndex
            ExpandTildeInContentIndex(nodeConfig.ContentIndex, homeDir);

            // Expand paths in FileStorage
            if (nodeConfig.FileStorage != null)
            {
                ExpandTildeInStorage(nodeConfig.FileStorage, homeDir);
            }

            // Expand paths in RepoStorage
            if (nodeConfig.RepoStorage != null)
            {
                ExpandTildeInStorage(nodeConfig.RepoStorage, homeDir);
            }

            // Expand paths in SearchIndexes
            foreach (var searchIndex in nodeConfig.SearchIndexes)
            {
                ExpandTildeInSearchIndex(searchIndex, homeDir);
            }
        }

        // Expand paths in EmbeddingsCache
        if (config.EmbeddingsCache != null)
        {
            ExpandTildeInCache(config.EmbeddingsCache, homeDir);
        }

        // Expand paths in LLMCache
        if (config.LLMCache != null)
        {
            ExpandTildeInCache(config.LLMCache, homeDir);
        }
    }

    /// <summary>
    /// Expands tilde in ContentIndex paths
    /// </summary>
    /// <param name="config"></param>
    /// <param name="homeDir"></param>
    private static void ExpandTildeInContentIndex(ContentIndexConfig config, string homeDir)
    {
        if (config is SqliteContentIndexConfig sqliteConfig)
        {
            sqliteConfig.Path = ExpandTilde(sqliteConfig.Path, homeDir);
        }
    }

    /// <summary>
    /// Expands tilde in Storage paths
    /// </summary>
    /// <param name="config"></param>
    /// <param name="homeDir"></param>
    private static void ExpandTildeInStorage(StorageConfig config, string homeDir)
    {
        if (config is DiskStorageConfig diskConfig)
        {
            diskConfig.Path = ExpandTilde(diskConfig.Path, homeDir);
        }
    }

    /// <summary>
    /// Expands tilde in SearchIndex paths
    /// </summary>
    /// <param name="config"></param>
    /// <param name="homeDir"></param>
    private static void ExpandTildeInSearchIndex(SearchIndexConfig config, string homeDir)
    {
        switch (config)
        {
            case FtsSearchIndexConfig ftsConfig when ftsConfig.Path != null:
                ftsConfig.Path = ExpandTilde(ftsConfig.Path, homeDir);
                break;
            case VectorSearchIndexConfig vectorConfig when vectorConfig.Path != null:
                vectorConfig.Path = ExpandTilde(vectorConfig.Path, homeDir);
                break;
            case GraphSearchIndexConfig graphConfig when graphConfig.Path != null:
                graphConfig.Path = ExpandTilde(graphConfig.Path, homeDir);
                break;
        }
    }

    /// <summary>
    /// Expands tilde in Cache paths
    /// </summary>
    /// <param name="config"></param>
    /// <param name="homeDir"></param>
    private static void ExpandTildeInCache(CacheConfig config, string homeDir)
    {
        if (config.Path != null)
        {
            config.Path = ExpandTilde(config.Path, homeDir);
        }
    }

    /// <summary>
    /// Expands tilde (~/ or ~\) at the start of a path to the home directory.
    /// Cross-platform: handles both forward slash (Unix/macOS) and backslash (Windows).
    /// </summary>
    /// <param name="path"></param>
    /// <param name="homeDir"></param>
    private static string ExpandTilde(string path, string homeDir)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        // Handle just ~ (home directory)
        if (path == "~")
        {
            return homeDir;
        }

        // Handle ~/ or ~\ (home directory with path separator)
        // Cross-platform: works with both / (Unix) and \ (Windows)
        if (path.Length >= 2 && path[0] == '~' &&
            (path[1] == Path.DirectorySeparatorChar ||
             path[1] == Path.AltDirectorySeparatorChar))
        {
            return Path.Combine(homeDir, path.Substring(2));
        }

        return path;
    }
}
