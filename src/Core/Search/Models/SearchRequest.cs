// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search.Models;

/// <summary>
/// Request for searching content across nodes and indexes.
/// All properties have sensible defaults - only Query is required.
/// </summary>
public sealed class SearchRequest
{
    /// <summary>
    /// The search query string.
    /// Supports both infix notation (SQL-like) and MongoDB JSON format.
    /// Format is auto-detected: starts with '{' = JSON, otherwise = infix.
    /// </summary>
    public required string Query { get; set; }

    // Node selection (Q8)

    /// <summary>
    /// Specific nodes to search.
    /// Empty = use config defaultNodes.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
    public string[] Nodes { get; set; } = [];

    /// <summary>
    /// Nodes to exclude from search.
    /// Applies after Nodes selection.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
    public string[] ExcludeNodes { get; set; } = [];

    // Index selection (Requirements #8)

    /// <summary>
    /// Specific indexes to search.
    /// Empty = all indexes.
    /// Supports "indexId" and "nodeId:indexId" syntax.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
    public string[] SearchIndexes { get; set; } = [];

    /// <summary>
    /// Indexes to exclude from search.
    /// Same syntax as SearchIndexes.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
    public string[] ExcludeIndexes { get; set; } = [];

    // Result control (Q3, Q12)

    /// <summary>
    /// Maximum number of results to return.
    /// Default: 20 (from config or SearchConstants).
    /// </summary>
    public int Limit { get; set; } = SearchConstants.DefaultLimit;

    /// <summary>
    /// Pagination offset (skip first N results).
    /// Default: 0 (start from beginning).
    /// </summary>
    public int Offset { get; set; } = 0;

    /// <summary>
    /// Minimum relevance score threshold (0.0-1.0).
    /// Results below this score are filtered out.
    /// Default: 0.3 (from config or SearchConstants).
    /// </summary>
    public float MinRelevance { get; set; } = SearchConstants.DefaultMinRelevance;

    /// <summary>
    /// Memory safety limit per node.
    /// Maximum results to retrieve from each node before reranking.
    /// Default: 1000 (from config or SearchConstants).
    /// Null = use config value.
    /// </summary>
    public int? MaxResultsPerNode { get; set; }

    // Weight overrides (Q10)

    /// <summary>
    /// Override node weights at query time.
    /// Key = node ID, Value = weight multiplier.
    /// Null = use config weights.
    /// </summary>
    public Dictionary<string, float>? NodeWeights { get; set; }

    // Content control (Q13, Q21)

    /// <summary>
    /// Return snippets instead of full content.
    /// Reduces I/O and response size.
    /// Default: false (return full content).
    /// </summary>
    public bool SnippetOnly { get; set; } = false;

    /// <summary>
    /// Override config snippet length.
    /// Null = use config value.
    /// </summary>
    public int? SnippetLength { get; set; }

    /// <summary>
    /// Override config max snippets per result.
    /// Null = use config value.
    /// </summary>
    public int? MaxSnippetsPerResult { get; set; }

    // Highlighting (Q20)

    /// <summary>
    /// Wrap matched terms in highlight markers.
    /// Accounts for stemming (only FTS index knows stem matches).
    /// Default: false.
    /// </summary>
    public bool Highlight { get; set; } = false;

    // Concurrency (Q2, Q11)

    /// <summary>
    /// Wait for pending index operations before searching.
    /// Ensures latest results at cost of latency.
    /// Default: false (eventual consistency).
    /// </summary>
    public bool WaitForIndexing { get; set; } = false;

    /// <summary>
    /// Override config search timeout per node (seconds).
    /// Null = use config value.
    /// </summary>
    public int? TimeoutSeconds { get; set; }
}
