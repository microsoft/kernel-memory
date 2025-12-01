// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search;

/// <summary>
/// Constants for search functionality.
/// Centralizes all magic values for maintainability.
/// </summary>
public static class SearchConstants
{
    /// <summary>
    /// Default minimum relevance score threshold (0.0-1.0).
    /// Results below this score are filtered out.
    /// </summary>
    public const float DefaultMinRelevance = 0.3f;

    /// <summary>
    /// Default maximum number of results to return per search.
    /// </summary>
    public const int DefaultLimit = 20;

    /// <summary>
    /// Default search timeout in seconds per node.
    /// </summary>
    public const int DefaultSearchTimeoutSeconds = 30;

    /// <summary>
    /// Default maximum results to retrieve from each node (memory safety).
    /// Prevents memory exhaustion from large result sets.
    /// </summary>
    public const int DefaultMaxResultsPerNode = 1000;

    /// <summary>
    /// Default node weight for relevance scoring.
    /// </summary>
    public const float DefaultNodeWeight = 1.0f;

    /// <summary>
    /// Default search index weight for relevance scoring.
    /// </summary>
    public const float DefaultIndexWeight = 1.0f;

    /// <summary>
    /// BM25 score normalization divisor for exponential mapping.
    /// Maps BM25 range [-10, 0] to [0.37, 1.0] using exp(score/divisor).
    /// </summary>
    public const double Bm25NormalizationDivisor = 10.0;

    /// <summary>
    /// Maximum nesting depth for query parentheses.
    /// Prevents DoS attacks via deeply nested queries.
    /// </summary>
    public const int MaxQueryDepth = 10;

    /// <summary>
    /// Maximum number of boolean operators (AND/OR/NOT) in a single query.
    /// Prevents query complexity attacks.
    /// </summary>
    public const int MaxBooleanOperators = 50;

    /// <summary>
    /// Maximum length of a field value in query (characters).
    /// Prevents oversized query values.
    /// </summary>
    public const int MaxFieldValueLength = 1000;

    /// <summary>
    /// Maximum time allowed for query parsing (milliseconds).
    /// Prevents regex catastrophic backtracking.
    /// </summary>
    public const int QueryParseTimeoutMs = 1000;

    /// <summary>
    /// Default snippet length in characters.
    /// </summary>
    public const int DefaultSnippetLength = 200;

    /// <summary>
    /// Default maximum number of snippets per result.
    /// </summary>
    public const int DefaultMaxSnippetsPerResult = 1;

    /// <summary>
    /// Default snippet separator between multiple snippets.
    /// </summary>
    public const string DefaultSnippetSeparator = "...";

    /// <summary>
    /// Default highlight prefix marker.
    /// </summary>
    public const string DefaultHighlightPrefix = "<mark>";

    /// <summary>
    /// Default highlight suffix marker.
    /// </summary>
    public const string DefaultHighlightSuffix = "</mark>";

    /// <summary>
    /// Diminishing returns multipliers for aggregating multiple appearances of same record.
    /// First appearance: 1.0 (full weight)
    /// Second appearance: 0.5 (50% boost)
    /// Third appearance: 0.25 (25% boost)
    /// Fourth appearance: 0.125 (12.5% boost)
    /// Each subsequent multiplier is half of the previous.
    /// </summary>
    public static readonly float[] DefaultDiminishingMultipliers = [1.0f, 0.5f, 0.25f, 0.125f];

    /// <summary>
    /// Wildcard character for "all nodes" in node selection.
    /// </summary>
    public const string AllNodesWildcard = "*";

    /// <summary>
    /// Maximum relevance score (scores are capped at this value).
    /// </summary>
    public const float MaxRelevanceScore = 1.0f;

    /// <summary>
    /// Minimum relevance score.
    /// </summary>
    public const float MinRelevanceScore = 0.0f;
}
