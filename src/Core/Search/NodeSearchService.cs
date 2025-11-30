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
            // Escape FTS5 special characters and quote the term
            var escapedText = this.EscapeFtsText(node.SearchText);

            // If specific field, prefix with field name (SQLite FTS5 syntax)
            if (node.Field != null && this.IsFtsField(node.Field.FieldPath))
            {
                return $"{node.Field.FieldPath}:{escapedText}";
            }

            // Default field: search all FTS fields (title, description, content)
            // FTS5 syntax: {title description content}:term
            return $"{{title description content}}:{escapedText}";
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
            // Only extract text search from Contains operator on FTS fields
            if (node.Operator == ComparisonOperator.Contains &&
                node.Field?.FieldPath != null &&
                this.IsFtsField(node.Field.FieldPath) &&
                node.Value != null)
            {
                var searchText = node.Value.AsString();
                var escapedText = this.EscapeFtsText(searchText);
                return $"{node.Field.FieldPath}:{escapedText}";
            }

            // Other comparison operators (==, !=, >=, etc.) are handled by LINQ filtering
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

        private string EscapeFtsText(string text)
        {
            // FTS5 special characters that need escaping: " * ( ) 
            // Wrap in quotes to handle spaces and special characters
            var escaped = text.Replace("\"", "\"\"", StringComparison.Ordinal); // Escape quotes by doubling
            return $"\"{escaped}\"";
        }
    }
}
