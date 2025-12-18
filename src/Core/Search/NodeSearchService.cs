// Copyright (c) Microsoft. All rights reserved.
using System.Diagnostics;
using KernelMemory.Core.Search.Models;
using KernelMemory.Core.Search.Query.Ast;
using KernelMemory.Core.Storage;

namespace KernelMemory.Core.Search;

/// <summary>
/// Result of FTS query extraction from the AST.
/// Contains the FTS query string for SQLite and a list of NOT terms for post-filtering.
/// SQLite FTS5 has limited NOT support (requires left operand), so NOT terms
/// are filtered via LINQ after FTS returns initial results.
/// </summary>
/// <param name="FtsQuery">The FTS5 query string for positive terms.</param>
/// <param name="NotTerms">Terms to exclude via LINQ post-filtering. Each term includes optional field info.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
public sealed record FtsQueryResult(string FtsQuery, NotTerm[] NotTerms);

/// <summary>
/// Represents a term that should be excluded from search results.
/// Used for LINQ post-filtering since SQLite FTS5 NOT has limitations.
/// </summary>
/// <param name="Term">The term to exclude.</param>
/// <param name="Field">Optional field to check (title/description/content). If null, checks all fields.</param>
public sealed record NotTerm(string Term, string? Field);

/// <summary>
/// Per-node search service.
/// Executes searches within a single node's indexes.
/// Handles query parsing, FTS query execution, and result filtering.
/// </summary>
public sealed class NodeSearchService
{
    private readonly string _nodeId;
    private readonly string _indexId;
    private readonly IFtsIndex _ftsIndex;
    private readonly IContentStorage _contentStorage;

    /// <summary>
    /// Initialize a new NodeSearchService.
    /// </summary>
    /// <param name="nodeId">The node ID this service operates on.</param>
    /// <param name="ftsIndex">The FTS index for this node.</param>
    /// <param name="contentStorage">The content storage for loading full records.</param>
    /// <param name="indexId">Optional index ID for this FTS index. Defaults to Constants.SearchDefaults.DefaultFtsIndexId.</param>
    public NodeSearchService(
        string nodeId,
        IFtsIndex ftsIndex,
        IContentStorage contentStorage,
        string indexId = Constants.SearchDefaults.DefaultFtsIndexId)
    {
        this._nodeId = nodeId;
        this._indexId = indexId;
        this._ftsIndex = ftsIndex;
        this._contentStorage = contentStorage;
    }

    /// <summary>
    /// Search this node using a parsed query AST.
    /// </summary>
    /// <param name="queryNode">The parsed query AST.</param>
    /// <param name="request">The search request with options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results from this node.</returns>
    public async Task<(SearchIndexResult[] Results, TimeSpan SearchTime)> SearchAsync(
        QueryNode queryNode,
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Apply timeout
            var timeout = request.TimeoutSeconds ?? Constants.SearchDefaults.DefaultSearchTimeoutSeconds;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeout));

            // Query the FTS index
            var maxResults = request.MaxResultsPerNode ?? Constants.SearchDefaults.DefaultMaxResultsPerNode;

            // Convert QueryNode to FTS query string and extract NOT terms for post-filtering
            var queryResult = this.ExtractFtsQuery(queryNode);

            // Search the FTS index
            var ftsMatches = await this._ftsIndex.SearchAsync(
                queryResult.FtsQuery,
                maxResults,
                cts.Token).ConfigureAwait(false);

            // Load full ContentRecords from storage
            var results = new List<SearchIndexResult>();
            foreach (var match in ftsMatches)
            {
                var content = await this._contentStorage.GetByIdAsync(match.ContentId, cts.Token).ConfigureAwait(false);
                if (content != null)
                {
                    results.Add(new SearchIndexResult
                    {
                        RecordId = content.Id,
                        NodeId = this._nodeId,
                        IndexId = this._indexId,
                        ChunkId = null,
                        BaseRelevance = (float)match.Score,
                        Title = content.Title,
                        Description = content.Description,
                        Content = content.Content,
                        CreatedAt = content.ContentCreatedAt,
                        MimeType = content.MimeType,
                        Tags = content.Tags ?? [],
                        Metadata = content.Metadata ?? new Dictionary<string, string>()
                    });
                }
            }

            // Apply NOT term filtering via LINQ (SQLite FTS5 NOT has limitations)
            // Filter out any documents that contain the NOT terms
            if (queryResult.NotTerms.Length > 0)
            {
                results = this.ApplyNotTermFiltering(results, queryResult.NotTerms);
            }

            stopwatch.Stop();
            return ([.. results], stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            throw new Exceptions.SearchException(
                $"Node '{this._nodeId}' search timed out after {stopwatch.Elapsed.TotalSeconds:F2} seconds",
                Exceptions.SearchErrorType.NodeTimeout,
                this._nodeId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            throw new Exceptions.SearchException(
                $"Failed to search node '{this._nodeId}': {ex.Message}",
                Exceptions.SearchErrorType.NodeUnavailable,
                this._nodeId);
        }
    }

    /// <summary>
    /// Apply NOT term filtering to results via LINQ.
    /// Excludes documents that contain any of the NOT terms.
    /// </summary>
    /// <param name="results">The search results to filter.</param>
    /// <param name="notTerms">The terms to exclude.</param>
    /// <returns>Filtered results excluding documents containing NOT terms.</returns>
    private List<SearchIndexResult> ApplyNotTermFiltering(List<SearchIndexResult> results, NotTerm[] notTerms)
    {
        return results
            .Where(result => !this.ContainsAnyNotTerm(result, notTerms))
            .ToList();
    }

    /// <summary>
    /// Check if a result contains any of the NOT terms.
    /// </summary>
    /// <param name="result">The search result to check.</param>
    /// <param name="notTerms">The NOT terms to check for.</param>
    /// <returns>True if the result contains any NOT term.</returns>
    private bool ContainsAnyNotTerm(SearchIndexResult result, NotTerm[] notTerms)
    {
        foreach (var notTerm in notTerms)
        {
            if (this.ContainsNotTerm(result, notTerm))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if a result contains a specific NOT term.
    /// </summary>
    /// <param name="result">The search result to check.</param>
    /// <param name="notTerm">The NOT term to check for.</param>
    /// <returns>True if the result contains the NOT term.</returns>
    private bool ContainsNotTerm(SearchIndexResult result, NotTerm notTerm)
    {
        // Case-insensitive contains check
        var term = notTerm.Term;

        // Check specific field if specified
        if (notTerm.Field != null)
        {
            var fieldValue = notTerm.Field.ToLowerInvariant() switch
            {
                "title" => result.Title ?? string.Empty,
                "description" => result.Description ?? string.Empty,
                "content" => result.Content ?? string.Empty,
                _ => string.Empty
            };

            return fieldValue.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        // Check all FTS fields (title, description, content)
        var title = result.Title ?? string.Empty;
        var description = result.Description ?? string.Empty;
        var content = result.Content ?? string.Empty;

        return title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               content.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extract FTS query string and NOT terms from query AST.
    /// Converts the AST to SQLite FTS5 query syntax for positive terms.
    /// NOT terms are collected separately for LINQ post-filtering.
    /// </summary>
    private FtsQueryResult ExtractFtsQuery(QueryNode queryNode)
    {
        var visitor = new FtsQueryExtractor();
        return visitor.Extract(queryNode);
    }

    /// <summary>
    /// Visitor that extracts FTS query terms from the AST.
    /// Focuses only on TextSearchNode and field-specific text searches.
    /// Logical operators are preserved for FTS query syntax.
    /// NOT operators are handled specially - their terms are collected for LINQ post-filtering.
    /// </summary>
    private sealed class FtsQueryExtractor
    {
        private readonly List<NotTerm> _notTerms = [];

        /// <summary>
        /// SQLite FTS5 reserved words that must be quoted when used as search terms.
        /// These keywords have special meaning in FTS5 query syntax.
        /// </summary>
        private static readonly HashSet<string> s_fts5ReservedWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "AND", "OR", "NOT", "NEAR"
        };

        public FtsQueryResult Extract(QueryNode node)
        {
            var terms = this.ExtractTerms(node);

            // If only NOT terms exist (no positive terms), use wildcard to get all documents
            // then filter with NOT terms
            var ftsQuery = string.IsNullOrEmpty(terms) ? "*" : terms;

            return new FtsQueryResult(ftsQuery, [.. this._notTerms]);
        }

        private string ExtractTerms(QueryNode node)
        {
            return node switch
            {
                TextSearchNode textNode => this.ExtractTextSearch(textNode),
                LogicalNode logicalNode => this.ExtractLogical(logicalNode),
                ComparisonNode comparisonNode => this.ExtractComparison(comparisonNode),
                _ => string.Empty
            };
        }

        private string ExtractTextSearch(TextSearchNode node)
        {
            // Check if this is a phrase search (contains spaces)
            var isPhrase = node.SearchText.Contains(' ', StringComparison.Ordinal);

            if (isPhrase)
            {
                // Phrase searches: use quotes and no field prefix
                // FTS5 doesn't support field:phrase syntax well, so just search all fields
                var escapedPhrase = this.EscapeFtsPhrase(node.SearchText);
                return $"\"{escapedPhrase}\"";
            }

            // Check if the term is a reserved word that needs quoting
            if (this.IsFts5ReservedWord(node.SearchText))
            {
                // Reserved words must be quoted to be treated as literal search terms
                // We cannot use field prefix with quoted terms in FTS5, so search all fields
                var escapedTerm = this.EscapeFtsPhrase(node.SearchText);
                return $"\"{escapedTerm}\"";
            }

            // Single word searches: use field prefix WITHOUT quotes
            var escaped = this.EscapeFtsSingleTerm(node.SearchText);

            // If specific field, prefix with field name (SQLite FTS5 syntax)
            if (node.Field != null && this.IsFtsField(node.Field.FieldPath))
            {
                return $"{node.Field.FieldPath}:{escaped}";
            }

            // Default field: search all FTS fields (title, description, content)
            // FTS5 syntax: {title description content}:term
            return $"{{title description content}}:{escaped}";
        }

        private string ExtractLogical(LogicalNode node)
        {
            // Handle NOT and NOR specially - collect terms for LINQ post-filtering
            if (node.Operator == LogicalOperator.Not || node.Operator == LogicalOperator.Nor)
            {
                this.CollectNotTerms(node);
                // Return empty string - NOT terms are not included in FTS query
                return string.Empty;
            }

            var childTerms = node.Children
                .Select(this.ExtractTerms)
                .Where(t => !string.IsNullOrEmpty(t))
                .ToArray();

            if (childTerms.Length == 0)
            {
                return string.Empty;
            }

            return node.Operator switch
            {
                LogicalOperator.And => string.Join(" AND ", childTerms.Select(t => $"({t})")),
                LogicalOperator.Or => string.Join(" OR ", childTerms.Select(t => $"({t})")),
                _ => string.Empty
            };
        }

        /// <summary>
        /// Collect NOT terms from a NOT or NOR node.
        /// These terms will be filtered via LINQ after FTS returns results.
        /// </summary>
        private void CollectNotTerms(LogicalNode node)
        {
            foreach (var child in node.Children)
            {
                this.CollectNotTermsFromNode(child);
            }
        }

        /// <summary>
        /// Recursively collect NOT terms from a node.
        /// </summary>
        private void CollectNotTermsFromNode(QueryNode node)
        {
            switch (node)
            {
                case TextSearchNode textNode:
                    // Extract the term and optional field
                    this._notTerms.Add(new NotTerm(textNode.SearchText, textNode.Field?.FieldPath));
                    break;

                case ComparisonNode comparisonNode:
                    // Handle field:value comparisons for NOT
                    if ((comparisonNode.Operator == ComparisonOperator.Contains ||
                         comparisonNode.Operator == ComparisonOperator.Equal) &&
                        comparisonNode.Field?.FieldPath != null &&
                        comparisonNode.Value != null)
                    {
                        var term = comparisonNode.Value.AsString();
                        this._notTerms.Add(new NotTerm(term, comparisonNode.Field.FieldPath));
                    }

                    break;

                case LogicalNode logicalNode:
                    // Recursively collect from nested logical nodes
                    // For nested NOT/NOR, we add all children as NOT terms
                    // For nested AND/OR within NOT, all their children become NOT terms
                    foreach (var child in logicalNode.Children)
                    {
                        this.CollectNotTermsFromNode(child);
                    }

                    break;
            }
        }

        private string ExtractComparison(ComparisonNode node)
        {
            // Extract text search from Contains OR Equal operator on FTS fields
            // Equal on FTS fields uses FTS semantics (substring/stemming match), not exact equality
            if ((node.Operator == ComparisonOperator.Contains || node.Operator == ComparisonOperator.Equal) &&
                node.Field?.FieldPath != null &&
                this.IsFtsField(node.Field.FieldPath) &&
                node.Value != null)
            {
                var searchText = node.Value.AsString();
                var isPhrase = searchText.Contains(' ', StringComparison.Ordinal);

                if (isPhrase)
                {
                    // Phrase search: use quotes without field prefix
                    var escapedPhrase = this.EscapeFtsPhrase(searchText);
                    return $"\"{escapedPhrase}\"";
                }

                // Check if the term is a reserved word that needs quoting
                if (this.IsFts5ReservedWord(searchText))
                {
                    // Reserved words must be quoted to be treated as literal search terms
                    // We cannot use field prefix with quoted terms in FTS5
                    var escapedTerm = this.EscapeFtsPhrase(searchText);
                    return $"\"{escapedTerm}\"";
                }

                // Single word: use field prefix without quotes
                var escaped = this.EscapeFtsSingleTerm(searchText);
                return $"{node.Field.FieldPath}:{escaped}";
            }

            // Other comparison operators (!=, >=, <, etc.) are handled by LINQ filtering
            // Return empty string as these don't contribute to FTS query
            return string.Empty;
        }

        private bool IsFtsField(string? fieldPath)
        {
            if (fieldPath == null)
            {
                return false;
            }

            var normalized = fieldPath.ToLowerInvariant();
            return normalized == "title" || normalized == "description" || normalized == "content";
        }

        /// <summary>
        /// Check if a term is an FTS5 reserved word.
        /// Reserved words need special escaping to be searched as literals.
        /// </summary>
        private bool IsFts5ReservedWord(string term)
        {
            return s_fts5ReservedWords.Contains(term);
        }

        /// <summary>
        /// Escape a phrase for FTS5 quoted string search.
        /// Doubles any internal quotes (FTS5 escape syntax).
        /// </summary>
        private string EscapeFtsPhrase(string phrase)
        {
            return phrase.Replace("\"", "\"\"", StringComparison.Ordinal);
        }

        private string EscapeFtsSingleTerm(string term)
        {
            // For single-word searches with field prefix (e.g., content:call)
            // FTS5 does NOT support quotes after the colon: content:"call" is INVALID
            // We must use: content:call
            //
            // Escape FTS5 special characters: " *
            // For now, keep it simple: just remove quotes and wildcards that could break syntax
            return term.Replace("\"", string.Empty, StringComparison.Ordinal)
                      .Replace("*", string.Empty, StringComparison.Ordinal);
        }
    }
}
