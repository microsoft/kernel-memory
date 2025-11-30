// Copyright (c) Microsoft. All rights reserved.
using System.Diagnostics;
using KernelMemory.Core.Search.Models;
using KernelMemory.Core.Search.Query.Parsers;
using KernelMemory.Core.Search.Reranking;

namespace KernelMemory.Core.Search;

/// <summary>
/// Main search service implementation.
/// Orchestrates multi-node searches, result merging, and reranking.
/// Transport-agnostic: used by CLI, Web API, and RPC.
/// </summary>
public sealed class SearchService : ISearchService
{
    private readonly Dictionary<string, NodeSearchService> _nodeServices;
    private readonly ISearchReranker _reranker;

    /// <summary>
    /// Initialize a new SearchService.
    /// </summary>
    /// <param name="nodeServices">Per-node search services.</param>
    /// <param name="reranker">Reranking implementation (default: WeightedDiminishingReranker).</param>
    public SearchService(
        Dictionary<string, NodeSearchService> nodeServices,
        ISearchReranker? reranker = null)
    {
        this._nodeServices = nodeServices;
        this._reranker = reranker ?? new WeightedDiminishingReranker();
    }

    /// <summary>
    /// Execute a search query across configured nodes and indexes.
    /// </summary>
    public async Task<SearchResponse> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var totalStopwatch = Stopwatch.StartNew();

        // Parse the query
        var queryNode = QueryParserFactory.Parse(request.Query);

        // Determine which nodes to search
        var nodesToSearch = this.DetermineNodesToSearch(request);

        // Validate nodes exist and are accessible
        this.ValidateNodes(nodesToSearch);

        // Execute searches in parallel across all nodes
        var searchTasks = nodesToSearch.Select(nodeId =>
            this.SearchNodeAsync(nodeId, queryNode, request, cancellationToken));

        var nodeResults = await Task.WhenAll(searchTasks).ConfigureAwait(false);

        // Collect all results and timings
        var allResults = nodeResults.SelectMany(r => r.Results).ToArray();
        var nodeTimings = nodeResults.Select(r => new NodeTiming
        {
            NodeId = r.NodeId,
            SearchTime = r.SearchTime
        }).ToArray();

        // Build reranking config
        var rerankingConfig = this.BuildRerankingConfig(request, nodesToSearch);

        // Rerank results
        var rerankedResults = this._reranker.Rerank(allResults, rerankingConfig);

        // Apply min relevance filter
        var filtered = rerankedResults
            .Where(r => r.Relevance >= request.MinRelevance)
            .ToArray();

        // Apply pagination
        var paginated = filtered
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToArray();

        totalStopwatch.Stop();

        // Build response
        return new SearchResponse
        {
            Query = request.Query,
            TotalResults = paginated.Length,
            Results = paginated,
            Metadata = new SearchMetadata
            {
                NodesSearched = nodesToSearch.Length,
                NodesRequested = nodesToSearch.Length,
                ExecutionTime = totalStopwatch.Elapsed,
                NodeTimings = nodeTimings,
                Warnings = []
            }
        };
    }

    /// <summary>
    /// Validate a query without executing it.
    /// </summary>
    public Task<QueryValidationResult> ValidateQueryAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to parse the query
            QueryParserFactory.Parse(query);

            return Task.FromResult(new QueryValidationResult
            {
                IsValid = true,
                ErrorMessage = null,
                ErrorPosition = null,
                AvailableFields = ["id", "title", "description", "content", "tags", "metadata.*", "mimeType", "createdAt"]
            });
        }
        catch (QuerySyntaxException ex)
        {
            return Task.FromResult(new QueryValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message,
                ErrorPosition = ex.Position,
                AvailableFields = ["id", "title", "description", "content", "tags", "metadata.*", "mimeType", "createdAt"]
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new QueryValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Query validation failed: {ex.Message}",
                ErrorPosition = null,
                AvailableFields = ["id", "title", "description", "content", "tags", "metadata.*", "mimeType", "createdAt"]
            });
        }
    }

    /// <summary>
    /// Search a single node.
    /// </summary>
    private async Task<(string NodeId, SearchIndexResult[] Results, TimeSpan SearchTime)> SearchNodeAsync(
        string nodeId,
        Query.Ast.QueryNode queryNode,
        SearchRequest request,
        CancellationToken cancellationToken)
    {
        var nodeService = this._nodeServices[nodeId];
        var (results, searchTime) = await nodeService.SearchAsync(queryNode, request, cancellationToken).ConfigureAwait(false);
        return (nodeId, results, searchTime);
    }

    /// <summary>
    /// Determine which nodes to search based on request and defaults.
    /// </summary>
    private string[] DetermineNodesToSearch(SearchRequest request)
    {
        // If specific nodes requested, use those
        if (request.Nodes.Length > 0)
        {
            var nodes = request.Nodes.Except(request.ExcludeNodes).ToArray();
            if (nodes.Length == 0)
            {
                throw new Exceptions.SearchException(
                    "No nodes to search after applying exclusions",
                    Exceptions.SearchErrorType.InvalidConfiguration);
            }
            return nodes;
        }

        // Otherwise, use all configured nodes minus exclusions
        var allNodes = this._nodeServices.Keys.Except(request.ExcludeNodes).ToArray();
        if (allNodes.Length == 0)
        {
            throw new Exceptions.SearchException(
                "No nodes to search - all nodes excluded",
                Exceptions.SearchErrorType.InvalidConfiguration);
        }

        return allNodes;
    }

    /// <summary>
    /// Validate that requested nodes exist and are accessible.
    /// </summary>
    private void ValidateNodes(string[] nodeIds)
    {
        foreach (var nodeId in nodeIds)
        {
            if (!this._nodeServices.ContainsKey(nodeId))
            {
                throw new Exceptions.SearchException(
                    $"Node '{nodeId}' not found in configuration",
                    Exceptions.SearchErrorType.NodeNotFound,
                    nodeId);
            }
        }
    }

    /// <summary>
    /// Build reranking configuration from request and defaults.
    /// </summary>
    private RerankingConfig BuildRerankingConfig(SearchRequest request, string[] nodeIds)
    {
        // Node weights: use request overrides or defaults
        var nodeWeights = new Dictionary<string, float>();
        foreach (var nodeId in nodeIds)
        {
            if (request.NodeWeights?.TryGetValue(nodeId, out var weight) == true)
            {
                nodeWeights[nodeId] = weight;
            }
            else
            {
                nodeWeights[nodeId] = SearchConstants.DefaultNodeWeight;
            }
        }

        // Index weights: use defaults for now
        // TODO: Load from configuration
        var indexWeights = new Dictionary<string, Dictionary<string, float>>();
        foreach (var nodeId in nodeIds)
        {
            indexWeights[nodeId] = new Dictionary<string, float>
            {
                ["fts-main"] = SearchConstants.DefaultIndexWeight
            };
        }

        return new RerankingConfig
        {
            NodeWeights = nodeWeights,
            IndexWeights = indexWeights,
            DiminishingMultipliers = SearchConstants.DefaultDiminishingMultipliers
        };
    }
}
