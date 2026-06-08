// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Embeddings;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Config.Validation;

namespace KernelMemory.Core.Config.SearchIndex;

/// <summary>
/// Base class for search index configurations
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(FtsSearchIndexConfig), typeDiscriminator: "sqliteFTS")]
[JsonDerivedType(typeof(VectorSearchIndexConfig), typeDiscriminator: "sqliteVector")]
[JsonDerivedType(typeof(GraphSearchIndexConfig), typeDiscriminator: "graph")]
public abstract class SearchIndexConfig : IValidatable
{
    /// <summary>
    /// Unique identifier for this search index instance.
    /// Must be unique within a node's SearchIndexes array.
    /// Used to identify index instances in operations pipeline (stable across config changes).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Type of search index
    /// </summary>
    [JsonIgnore]
    public SearchIndexTypes Type { get; set; }

    /// <summary>
    /// Weight for relevance scoring when searching across multiple indexes.
    /// Higher weight = results from this index ranked higher.
    /// Default: 1.0 (neutral weight).
    /// Range: 0.0 (exclude) to any positive value.
    /// </summary>
    [JsonPropertyName("weight")]
    public float Weight { get; set; } = 1.0f;

    /// <summary>
    /// Whether this index is required for search operations.
    /// If true and index is unavailable, search fails with error.
    /// If false and index is unavailable, search continues with warning.
    /// Default: false (optional index - best effort).
    /// </summary>
    [JsonPropertyName("required")]
    public bool Required { get; set; } = false;

    /// <summary>
    /// Optional embeddings configuration for this index
    /// Overrides node-level or global embeddings config
    /// </summary>
    [JsonPropertyName("embeddings")]
    public EmbeddingsConfig? Embeddings { get; set; }

    /// <summary>
    /// Validates the search index configuration
    /// </summary>
    /// <param name="path"></param>
    public abstract void Validate(string path);
}
