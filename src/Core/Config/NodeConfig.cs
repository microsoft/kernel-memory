using System.Text.Json.Serialization;
using KernelMemory.Core.Config.ContentIndex;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Config.SearchIndex;
using KernelMemory.Core.Config.Storage;
using KernelMemory.Core.Config.Validation;

namespace KernelMemory.Core.Config;

/// <summary>
/// Configuration for a single memory node
/// </summary>
public sealed class NodeConfig : IValidatable
{
    /// <summary>
    /// Unique identifier for this node
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Access level for this node
    /// </summary>
    [JsonPropertyName("access")]
    public NodeAccessLevels Access { get; set; } = NodeAccessLevels.Full;

    /// <summary>
    /// Content index (source of truth) - REQUIRED
    /// Stores metadata, cached content, and ingestion state
    /// </summary>
    [JsonPropertyName("contentIndex")]
    public ContentIndexConfig ContentIndex { get; set; } = null!;

    /// <summary>
    /// Optional file storage for binary files
    /// </summary>
    [JsonPropertyName("fileStorage")]
    public StorageConfig? FileStorage { get; set; }

    /// <summary>
    /// Optional repository storage for git repositories
    /// </summary>
    [JsonPropertyName("repoStorage")]
    public StorageConfig? RepoStorage { get; set; }

    /// <summary>
    /// Search indexes for content retrieval
    /// Multiple indexes can be used for different search strategies
    /// </summary>
    [JsonPropertyName("searchIndexes")]
    public List<SearchIndexConfig> SearchIndexes { get; set; } = new();

    /// <summary>
    /// Validates the node configuration
    /// </summary>
    /// <param name="path"></param>
    public void Validate(string path)
    {
        if (string.IsNullOrWhiteSpace(this.Id))
        {
            throw new ConfigException(path, "Node ID is required");
        }

        if (this.ContentIndex == null)
        {
            throw new ConfigException($"{path}.ContentIndex", "ContentIndex is required");
        }

        this.ContentIndex.Validate($"{path}.ContentIndex");
        this.FileStorage?.Validate($"{path}.FileStorage");
        this.RepoStorage?.Validate($"{path}.RepoStorage");

        for (int i = 0; i < this.SearchIndexes.Count; i++)
        {
            this.SearchIndexes[i].Validate($"{path}.SearchIndexes[{i}]");
        }
    }

    /// <summary>
    /// Creates a default "personal" node configuration
    /// </summary>
    /// <param name="nodeDir"></param>
    internal static NodeConfig CreateDefaultPersonalNode(string nodeDir)
    {
        return new NodeConfig
        {
            Id = "personal",
            Access = NodeAccessLevels.Full,
            ContentIndex = new SqliteContentIndexConfig
            {
                Path = Path.Combine(nodeDir, "content.db")
            },
            FileStorage = null,
            RepoStorage = null,
            SearchIndexes = new List<SearchIndexConfig>
            {
                new FtsSearchIndexConfig
                {
                    Type = SearchIndexTypes.SqliteFTS,
                    Path = Path.Combine(nodeDir, "fts.db"),
                    EnableStemming = true
                },
                new VectorSearchIndexConfig
                {
                    Type = SearchIndexTypes.SqliteVector,
                    Path = Path.Combine(nodeDir, "vectors.db"),
                    Dimensions = 768,
                    Metric = VectorMetrics.Cosine
                }
            }
        };
    }
}
