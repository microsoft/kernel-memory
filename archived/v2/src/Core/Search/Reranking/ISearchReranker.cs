// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Search.Models;

namespace KernelMemory.Core.Search.Reranking;

/// <summary>
/// Interface for search result reranking implementations.
/// Rerankers combine results from multiple indexes/nodes and apply relevance scoring.
/// Allows custom reranking strategies to be injected via DI.
/// </summary>
public interface ISearchReranker
{
    /// <summary>
    /// Rerank search results from multiple indexes/nodes.
    /// Handles duplicate records across indexes with diminishing returns.
    /// </summary>
    /// <param name="results">Raw results from all indexes (may contain duplicates).</param>
    /// <param name="config">Reranking configuration (weights, diminishing factors).</param>
    /// <returns>Reranked and merged results (duplicates aggregated).</returns>
    SearchResult[] Rerank(SearchIndexResult[] results, RerankingConfig config);
}
