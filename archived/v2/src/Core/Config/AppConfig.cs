// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Cache;
using KernelMemory.Core.Config.Validation;
using KernelMemory.Core.Logging;

namespace KernelMemory.Core.Config;

/// <summary>
/// Root configuration for Kernel Memory application
/// Loaded from ~/.km/config.json or custom path
/// </summary>
#pragma warning disable CA1724 // Conflicts with Microsoft.Identity.Client.AppConfig when Azure.Identity is referenced.
public sealed class AppConfig : IValidatable
#pragma warning restore CA1724
{
    /// <summary>
    /// Named memory nodes (e.g., "personal", "work")
    /// Key is the node ID, value is the node configuration
    /// </summary>
    [JsonPropertyName("nodes")]
    public Dictionary<string, NodeConfig> Nodes { get; set; } = new();

    /// <summary>
    /// Optional cache for embeddings to reduce API calls
    /// </summary>
    [JsonPropertyName("embeddingsCache")]
    public CacheConfig? EmbeddingsCache { get; set; }

    /// <summary>
    /// Optional cache for LLM responses
    /// </summary>
    [JsonPropertyName("llmCache")]
    public CacheConfig? LLMCache { get; set; }

    /// <summary>
    /// Global search configuration settings
    /// </summary>
    [JsonPropertyName("search")]
    public SearchConfig? Search { get; set; }

    /// <summary>
    /// Logging configuration settings
    /// Controls log level, file output, and format
    /// </summary>
    [JsonPropertyName("logging")]
    public LoggingConfig? Logging { get; set; }

    /// <summary>
    /// Validates the entire configuration tree
    /// </summary>
    /// <param name="path"></param>
    public void Validate(string path = "")
    {
        if (this.Nodes.Count == 0)
        {
            throw new ConfigException("Nodes", "At least one node must be configured");
        }

        foreach (var (nodeId, nodeConfig) in this.Nodes)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                throw new ConfigException("Nodes", "Node ID cannot be empty");
            }

            nodeConfig.Validate($"Nodes.{nodeId}");
        }

        this.EmbeddingsCache?.Validate("EmbeddingsCache");
        this.LLMCache?.Validate("LLMCache");
        this.Search?.Validate("Search");
    }

    /// <summary>
    /// Creates a default configuration with a single "personal" node
    /// using local SQLite storage in the user's home directory
    /// </summary>
    public static AppConfig CreateDefault()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var kmDir = Path.Combine(homeDir, ".km");
        return CreateDefault(kmDir);
    }

    /// <summary>
    /// Creates a default configuration with a single "personal" node
    /// using local SQLite storage in the specified base directory.
    /// Includes embeddings cache for efficient vector search operations.
    /// </summary>
    /// <param name="baseDir">Base directory for data storage</param>
    public static AppConfig CreateDefault(string baseDir)
    {
        var personalNodeDir = Path.Combine(baseDir, "nodes", "personal");

        return new AppConfig
        {
            Nodes = new Dictionary<string, NodeConfig>
            {
                ["personal"] = NodeConfig.CreateDefaultPersonalNode(personalNodeDir)
            },
            EmbeddingsCache = CacheConfig.CreateDefaultSqliteCache(
                Path.Combine(baseDir, "embeddings-cache.db"))
            // LLMCache intentionally omitted - add when LLM features are implemented
        };
    }
}
