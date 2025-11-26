using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Cache;
using KernelMemory.Core.Config.Validation;

namespace KernelMemory.Core.Config;

/// <summary>
/// Root configuration for Kernel Memory application
/// Loaded from ~/.km/config.json or custom path
/// </summary>
public sealed class AppConfig : IValidatable
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
    }

    /// <summary>
    /// Creates a default configuration with a single "personal" node
    /// using local SQLite storage
    /// </summary>
    public static AppConfig CreateDefault()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var kmDir = Path.Combine(homeDir, ".km");
        var personalNodeDir = Path.Combine(kmDir, "nodes", "personal");

        return new AppConfig
        {
            Nodes = new Dictionary<string, NodeConfig>
            {
                ["personal"] = NodeConfig.CreateDefaultPersonalNode(personalNodeDir)
            },
            EmbeddingsCache = CacheConfig.CreateDefaultSqliteCache(
                Path.Combine(kmDir, "embeddings-cache.db")
            ),
            LLMCache = null
        };
    }
}
