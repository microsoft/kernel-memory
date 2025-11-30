// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Validation;
using KernelMemory.Core.Search;

namespace KernelMemory.Core.Config;

/// <summary>
/// Global search configuration settings.
/// Applied as defaults across all search operations unless overridden.
/// </summary>
public sealed class SearchConfig : IValidatable
{
    /// <summary>
    /// Default minimum relevance score threshold (0.0-1.0).
    /// Results below this score are filtered out.
    /// Default: 0.3 (moderate threshold).
    /// </summary>
    [JsonPropertyName("defaultMinRelevance")]
    public float DefaultMinRelevance { get; set; } = SearchConstants.DefaultMinRelevance;

    /// <summary>
    /// Default maximum number of results to return per search.
    /// Default: 20 results.
    /// </summary>
    [JsonPropertyName("defaultLimit")]
    public int DefaultLimit { get; set; } = SearchConstants.DefaultLimit;

    /// <summary>
    /// Search timeout in seconds per node.
    /// If a node takes longer than this, it times out and is excluded from results.
    /// Default: 30 seconds.
    /// </summary>
    [JsonPropertyName("searchTimeoutSeconds")]
    public int SearchTimeoutSeconds { get; set; } = SearchConstants.DefaultSearchTimeoutSeconds;

    /// <summary>
    /// Default maximum results to retrieve from each node (memory safety).
    /// Prevents memory exhaustion from large result sets.
    /// Results are sorted by (relevance DESC, createdAt DESC) before limiting.
    /// Default: 1000 results per node.
    /// </summary>
    [JsonPropertyName("maxResultsPerNode")]
    public int MaxResultsPerNode { get; set; } = SearchConstants.DefaultMaxResultsPerNode;

    /// <summary>
    /// Default nodes to search when no explicit --nodes flag is provided.
    /// Use ["*"] to search all configured nodes (default).
    /// Use specific node IDs like ["personal", "work"] to limit search scope.
    /// </summary>
    [JsonPropertyName("defaultNodes")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
    public string[] DefaultNodes { get; set; } = [SearchConstants.AllNodesWildcard];

    /// <summary>
    /// Nodes to exclude from search by default.
    /// These nodes are never searched unless explicitly requested via --nodes flag.
    /// Default: empty (no exclusions).
    /// </summary>
    [JsonPropertyName("excludeNodes")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
    public string[] ExcludeNodes { get; set; } = [];

    /// <summary>
    /// Maximum nesting depth for query parentheses.
    /// Prevents DoS attacks via deeply nested queries.
    /// Default: 10 levels.
    /// </summary>
    [JsonPropertyName("maxQueryDepth")]
    public int MaxQueryDepth { get; set; } = SearchConstants.MaxQueryDepth;

    /// <summary>
    /// Maximum number of boolean operators (AND/OR/NOT) in a single query.
    /// Prevents query complexity attacks.
    /// Default: 50 operators.
    /// </summary>
    [JsonPropertyName("maxBooleanOperators")]
    public int MaxBooleanOperators { get; set; } = SearchConstants.MaxBooleanOperators;

    /// <summary>
    /// Maximum length of a field value in query (characters).
    /// Prevents oversized query values.
    /// Default: 1000 characters.
    /// </summary>
    [JsonPropertyName("maxFieldValueLength")]
    public int MaxFieldValueLength { get; set; } = SearchConstants.MaxFieldValueLength;

    /// <summary>
    /// Maximum time allowed for query parsing (milliseconds).
    /// Prevents regex catastrophic backtracking.
    /// Default: 1000ms (1 second).
    /// </summary>
    [JsonPropertyName("queryParseTimeoutMs")]
    public int QueryParseTimeoutMs { get; set; } = SearchConstants.QueryParseTimeoutMs;

    /// <summary>
    /// Default snippet length in characters when --snippet flag is used.
    /// Default: 200 characters.
    /// </summary>
    [JsonPropertyName("snippetLength")]
    public int SnippetLength { get; set; } = SearchConstants.DefaultSnippetLength;

    /// <summary>
    /// Default maximum number of snippets per result when --snippet flag is used.
    /// Default: 1 snippet.
    /// </summary>
    [JsonPropertyName("maxSnippetsPerResult")]
    public int MaxSnippetsPerResult { get; set; } = SearchConstants.DefaultMaxSnippetsPerResult;

    /// <summary>
    /// Separator string between multiple snippets.
    /// Default: "..." (ellipsis).
    /// </summary>
    [JsonPropertyName("snippetSeparator")]
    public string SnippetSeparator { get; set; } = SearchConstants.DefaultSnippetSeparator;

    /// <summary>
    /// Prefix marker for highlighting matched terms.
    /// Default: "&lt;mark&gt;" (HTML-style).
    /// </summary>
    [JsonPropertyName("highlightPrefix")]
    public string HighlightPrefix { get; set; } = SearchConstants.DefaultHighlightPrefix;

    /// <summary>
    /// Suffix marker for highlighting matched terms.
    /// Default: "&lt;/mark&gt;" (HTML-style).
    /// </summary>
    [JsonPropertyName("highlightSuffix")]
    public string HighlightSuffix { get; set; } = SearchConstants.DefaultHighlightSuffix;

    /// <summary>
    /// Validates the search configuration.
    /// </summary>
    /// <param name="path">Configuration path for error reporting.</param>
    public void Validate(string path)
    {
        // Validate min relevance score
        if (this.DefaultMinRelevance < SearchConstants.MinRelevanceScore || this.DefaultMinRelevance > SearchConstants.MaxRelevanceScore)
        {
            throw new ConfigException($"{path}.DefaultMinRelevance",
                $"Must be between {SearchConstants.MinRelevanceScore} and {SearchConstants.MaxRelevanceScore}");
        }

        // Validate default limit
        if (this.DefaultLimit <= 0)
        {
            throw new ConfigException($"{path}.DefaultLimit", "Must be greater than 0");
        }

        // Validate timeout
        if (this.SearchTimeoutSeconds <= 0)
        {
            throw new ConfigException($"{path}.SearchTimeoutSeconds", "Must be greater than 0");
        }

        // Validate max results per node
        if (this.MaxResultsPerNode <= 0)
        {
            throw new ConfigException($"{path}.MaxResultsPerNode", "Must be greater than 0");
        }

        // Validate default nodes
        if (this.DefaultNodes.Length == 0)
        {
            throw new ConfigException($"{path}.DefaultNodes",
                "Must specify at least one node or use '*' for all nodes");
        }

        // Validate no contradictory node configuration
        if (this.DefaultNodes.Length == 1 && this.DefaultNodes[0] == SearchConstants.AllNodesWildcard)
        {
            // Using wildcard - excludeNodes is OK
        }
        else
        {
            // Using specific nodes - check for contradictions
            var defaultNodesSet = new HashSet<string>(this.DefaultNodes, StringComparer.OrdinalIgnoreCase);
            var excludeNodesSet = new HashSet<string>(this.ExcludeNodes, StringComparer.OrdinalIgnoreCase);
            var conflicts = defaultNodesSet.Intersect(excludeNodesSet).ToArray();

            if (conflicts.Length > 0)
            {
                throw new ConfigException($"{path}.DefaultNodes",
                    $"Contradictory configuration: nodes [{string.Join(", ", conflicts)}] appear in both DefaultNodes and ExcludeNodes");
            }
        }

        // Validate query complexity limits
        if (this.MaxQueryDepth <= 0)
        {
            throw new ConfigException($"{path}.MaxQueryDepth", "Must be greater than 0");
        }

        if (this.MaxBooleanOperators <= 0)
        {
            throw new ConfigException($"{path}.MaxBooleanOperators", "Must be greater than 0");
        }

        if (this.MaxFieldValueLength <= 0)
        {
            throw new ConfigException($"{path}.MaxFieldValueLength", "Must be greater than 0");
        }

        if (this.QueryParseTimeoutMs <= 0)
        {
            throw new ConfigException($"{path}.QueryParseTimeoutMs", "Must be greater than 0");
        }

        // Validate snippet settings
        if (this.SnippetLength <= 0)
        {
            throw new ConfigException($"{path}.SnippetLength", "Must be greater than 0");
        }

        if (this.MaxSnippetsPerResult <= 0)
        {
            throw new ConfigException($"{path}.MaxSnippetsPerResult", "Must be greater than 0");
        }

        if (string.IsNullOrEmpty(this.SnippetSeparator))
        {
            throw new ConfigException($"{path}.SnippetSeparator", "Cannot be null or empty");
        }

        if (string.IsNullOrEmpty(this.HighlightPrefix))
        {
            throw new ConfigException($"{path}.HighlightPrefix", "Cannot be null or empty");
        }

        if (string.IsNullOrEmpty(this.HighlightSuffix))
        {
            throw new ConfigException($"{path}.HighlightSuffix", "Cannot be null or empty");
        }
    }
}
