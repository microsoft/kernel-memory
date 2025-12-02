// Copyright (c) Microsoft. All rights reserved.
using System.Diagnostics;
using KernelMemory.Core.Search.Models;
using KernelMemory.Core.Search.Query.Ast;
using KernelMemory.Core.Storage;

namespace KernelMemory.Core.Search;

/// <summary>
/// Per-node search service.
/// Executes searches within a single node's indexes.
/// Handles query parsing, FTS query execution, and result filtering.
/// </summary>
public sealed class NodeSearchService
{
    private readonly string _nodeId;
    private readonly IFtsIndex _ftsIndex;
    private readonly IContentStorage _contentStorage;

    /// <summary>
    /// Initialize a new NodeSearchService.
    /// </summary>
    /// <param name="nodeId">The node ID this service operates on.</param>
    /// <param name="ftsIndex">The FTS index for this node.</param>
    /// <param name="contentStorage">The content storage for loading full records.</param>
    public NodeSearchService(string nodeId, IFtsIndex ftsIndex, IContentStorage contentStorage)
    {
        this._nodeId = nodeId;
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
            var timeout = request.TimeoutSeconds ?? SearchConstants.DefaultSearchTimeoutSeconds;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeout));

            // Query the FTS index
            var maxResults = request.MaxResultsPerNode ?? SearchConstants.DefaultMaxResultsPerNode;

            // Convert QueryNode to FTS query string
            var ftsQuery = this.ExtractFtsQuery(queryNode);

            // Search the FTS index
            var ftsMatches = await this._ftsIndex.SearchAsync(
                ftsQuery,
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
                        IndexId = "fts-main", // TODO: Get from index config
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
    /// Extract FTS query string from query AST.
    /// Converts the AST to SQLite FTS5 query syntax.
    /// Only includes text search terms; filtering is done via LINQ on results.
    /// </summary>
    private string ExtractFtsQuery(QueryNode queryNode)
    {
        var visitor = new FtsQueryExtractor();
        return visitor.Extract(queryNode);
    }

    /// <summary>
    /// Visitor that extracts FTS query terms from the AST.
    /// Focuses only on TextSearchNode and field-specific text searches.
    /// Logical operators are preserved for FTS query syntax.
    /// </summary>
    private sealed class FtsQueryExtractor
    {
        /// <summary>
        /// SQLite FTS5 reserved words that must be quoted when used as search terms.
        /// These keywords have special meaning in FTS5 query syntax.
        /// </summary>
        private static readonly HashSet<string> s_fts5ReservedWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "AND", "OR", "NOT", "NEAR"
        };

        public string Extract(QueryNode node)
        {
            var terms = this.ExtractTerms(node);
            return string.IsNullOrEmpty(terms) ? "*" : terms;
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
                LogicalOperator.Not => childTerms.Length > 0 ? $"NOT ({childTerms[0]})" : string.Empty,
                LogicalOperator.Nor => string.Join(" AND ", childTerms.Select(t => $"NOT ({t})")),
                _ => string.Empty
            };
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
