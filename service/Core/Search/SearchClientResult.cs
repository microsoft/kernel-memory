// Copyright (c) Microsoft. All rights reserved.

using System.Text;

namespace Microsoft.KernelMemory.Search;

internal enum SearchMode
{
    SearchMode = 0,
    AskMode = 1,
}

internal enum SearchState
{
    Continue = 0,
    SkipRecord = 1,
    Stop = 2
}

internal class SearchClientResult
{
    public SearchMode Mode { get; private init; }
    public SearchState State { get; set; }
    public int RecordCount { get; set; }

    // Use by in Search and Ask mode
    public MemoryAnswer AskResult { get; private init; } = new();
    public int MaxRecordCount { get; private init; }

    // Use by Ask mode
    public SearchResult SearchResult { get; private init; } = new();
    public StringBuilder Facts { get; } = new();
    public int FactsAvailableCount { get; set; }
    public int FactsUsedCount { get; set; }
    public int TokensAvailable { get; set; }

    /// <summary>
    /// Create new instance in Ask mode
    /// </summary>
    public static SearchClientResult AskResultInstance(string question, string emptyAnswer, int maxGroundingFacts, int tokensAvailable)
    {
        return new SearchClientResult
        {
            Mode = SearchMode.AskMode,
            TokensAvailable = tokensAvailable,
            MaxRecordCount = maxGroundingFacts,
            AskResult = new MemoryAnswer
            {
                Question = question,
                NoResult = true,
                NoResultReason = "No question provided",
                Result = emptyAnswer,
            }
        };
    }

    /// <summary>
    /// Create new instance in Search mode
    /// </summary>
    public static SearchClientResult SearchResultInstance(string query, int maxSearchResults)
    {
        return new SearchClientResult
        {
            Mode = SearchMode.SearchMode,
            MaxRecordCount = maxSearchResults,
            SearchResult = new SearchResult
            {
                Query = query,
                Results = []
            }
        };
    }

    /// <summary>
    /// Tell search client to skip the current memory record
    /// </summary>
    public SearchClientResult SkipRecord()
    {
        this.State = SearchState.SkipRecord;
        return this;
    }

    /// <summary>
    /// Tell search client to stop processing records and return a final result
    /// </summary>
    public SearchClientResult Stop()
    {
        this.State = SearchState.Stop;
        return this;
    }

    // Force factory methods
    private SearchClientResult() { }
}
