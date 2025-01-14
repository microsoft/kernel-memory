// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Context;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.Prompts;

namespace Microsoft.KernelMemory.Search;

[Experimental("KMEXP05")]
public sealed class SearchClient : ISearchClient
{
    private readonly IMemoryDb _memoryDb;
    private readonly ITextGenerator _textGenerator;
    private readonly SearchClientConfig _config;
    private readonly ILogger<SearchClient> _log;
    private readonly AnswerGenerator _answerGenerator;
    private readonly string _answerPrompt;

    public SearchClient(
        IMemoryDb memoryDb,
        ITextGenerator textGenerator,
        SearchClientConfig? config = null,
        IPromptProvider? promptProvider = null,
        IContentModeration? contentModeration = null,
        ILoggerFactory? loggerFactory = null)
    {
        this._memoryDb = memoryDb;
        this._textGenerator = textGenerator;
        this._config = config ?? new SearchClientConfig();
        this._config.Validate();

        promptProvider ??= new EmbeddedPromptProvider();
        this._answerPrompt = promptProvider.ReadPrompt(Constants.PromptNamesAnswerWithFacts);

        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<SearchClient>();

        if (this._memoryDb == null)
        {
            throw new KernelMemoryException("Search memory DB not configured");
        }

        if (this._textGenerator == null)
        {
            throw new KernelMemoryException("Text generator not configured");
        }

        this._answerGenerator = new AnswerGenerator(textGenerator, config, promptProvider, contentModeration, loggerFactory);
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> ListIndexesAsync(CancellationToken cancellationToken = default)
    {
        return this._memoryDb.GetIndexesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SearchResult> SearchAsync(
        string index,
        string query,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        int limit = -1,
        IContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0) { limit = this._config.MaxMatchesCount; }

        var result = SearchClientResult.SearchResultInstance(query, limit);

        if (string.IsNullOrWhiteSpace(query) && (filters == null || filters.Count == 0))
        {
            this._log.LogWarning("No query or filters provided");
            return result.SearchResult;
        }
#pragma warning disable CA2254
        this._log.LogTrace(string.IsNullOrEmpty(query)
            ? $"Fetching relevant memories by similarity, min relevance {minRelevance}"
            : "Fetching relevant memories by filtering only, no vector search");
#pragma warning restore CA2254

        IAsyncEnumerable<(MemoryRecord, double)> matches = string.IsNullOrEmpty(query)
            ? this._memoryDb.GetListAsync(index, filters, limit, false, cancellationToken).Select(memoryRecord => (memoryRecord, double.MinValue))
            : this._memoryDb.GetSimilarListAsync(index, text: query, filters, minRelevance, limit, false, cancellationToken);

        await foreach ((MemoryRecord memoryRecord, double recordRelevance) in matches.ConfigureAwait(false).WithCancellation(cancellationToken))
        {
            result.State = SearchState.Continue;
            result = this.ProcessMemoryRecord(result, index, memoryRecord, recordRelevance);

            if (result.State == SearchState.SkipRecord) { continue; }

            if (result.State == SearchState.Stop) { break; }
        }

        this._log.LogTrace("{Count} records processed", result.RecordCount);

        if (result.SearchResult.Results.Count == 0)
        {
            this._log.LogDebug("No memories found");
        }

        return result.SearchResult;
    }

    /// <inheritdoc />
    public async Task<MemoryAnswer> AskAsync(
        string index,
        string question,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        IContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var result = new MemoryAnswer();

        var stream = this.AskStreamingAsync(
                index: index, question: question, filters, minRelevance, context, cancellationToken)
            .ConfigureAwait(false);

        var done = false;
        StringBuilder text = new(result.Result);
        await foreach (var part in stream.ConfigureAwait(false))
        {
            if (done) { break; }

            result.TokenUsage = part.TokenUsage;

            switch (part.StreamState)
            {
                case StreamStates.Error:
                    text.Clear();
                    result = part;

                    done = true;
                    break;

                case StreamStates.Reset:
                    text.Clear();
                    text.Append(part.Result);
                    result = part;
                    break;

                case StreamStates.Append:
                    result.NoResult = part.NoResult;
                    result.NoResultReason = part.NoResultReason;

                    text.Append(part.Result);
                    result.Result = text.ToString();

                    if (result.RelevantSources != null && part.RelevantSources != null)
                    {
                        result.RelevantSources = result.RelevantSources.Union(part.RelevantSources).ToList();
                    }

                    break;

                case StreamStates.Last:
                    result.NoResult = part.NoResult;
                    result.NoResultReason = part.NoResultReason;

                    text.Append(part.Result);
                    result.Result = text.ToString();

                    if (result.RelevantSources != null && part.RelevantSources != null)
                    {
                        result.RelevantSources = result.RelevantSources.Union(part.RelevantSources).ToList();
                    }

                    done = true;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(part.StreamState));
            }
        }

        result.Question = question;
        result.StreamState = null;
        return result;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<MemoryAnswer> AskStreamingAsync(
        string index,
        string question,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        IContext? context = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string emptyAnswer = context.GetCustomEmptyAnswerTextOrDefault(this._config.EmptyAnswer);
        string answerPrompt = context.GetCustomRagPromptOrDefault(this._answerPrompt);
        int limit = context.GetCustomRagMaxMatchesCountOrDefault(this._config.MaxMatchesCount);
        bool includeDuplicateFacts = context.GetCustomRagIncludeDuplicateFactsOrDefault(this._config.IncludeDuplicateFacts);

        var maxTokens = this._config.MaxAskPromptSize > 0
            ? this._config.MaxAskPromptSize
            : this._textGenerator.MaxTokenTotal;

        // Prepare results (empty, error, etc.)
        SearchClientResult result = SearchClientResult.AskResultInstance(
            question: question,
            emptyAnswer: emptyAnswer,
            moderatedAnswer: this._config.ModeratedAnswer,
            maxGroundingFacts: limit,
            tokensAvailable: maxTokens
                             - this._textGenerator.CountTokens(answerPrompt)
                             - this._textGenerator.CountTokens(question)
                             - this._config.AnswerTokens
        );

        if (string.IsNullOrEmpty(question))
        {
            this._log.LogWarning("No question provided");
            yield return result.NoQuestionResult;
            yield break;
        }

        this._log.LogTrace("Fetching relevant memories");
        IAsyncEnumerable<(MemoryRecord, double)> matches = this._memoryDb.GetSimilarListAsync(
            index: index,
            text: question,
            filters: filters,
            minRelevance: minRelevance,
            limit: limit,
            withEmbeddings: false,
            cancellationToken: cancellationToken);

        string factTemplate = context.GetCustomRagFactTemplateOrDefault(this._config.FactTemplate);
        if (!factTemplate.EndsWith('\n')) { factTemplate += "\n"; }

        // Memories are sorted by relevance, starting from the most relevant
        await foreach ((MemoryRecord memoryRecord, double recordRelevance) in matches.ConfigureAwait(false))
        {
            result.State = SearchState.Continue;
            result = this.ProcessMemoryRecord(result, index, memoryRecord, recordRelevance, includeDuplicateFacts, factTemplate);

            if (result.State == SearchState.SkipRecord) { continue; }

            if (result.State == SearchState.Stop) { break; }
        }

        this._log.LogTrace("{Count} records processed", result.RecordCount);

        var first = true;
        await foreach (MemoryAnswer answer in this._answerGenerator.GenerateAnswerAsync(question, result, context, cancellationToken).ConfigureAwait(false))
        {
            yield return answer;

            if (first)
            {
                // Remove redundant data, sent only once in the first record, to reduce payload
                first = false;

                // Note: we keep the sources in the other collections (e.g. AskResult.ErrorResult.RelevantSources),
                //       so in case of a stream reset the sources are sent again.
                result.AskResult.RelevantSources.Clear();
                result.AskResult.Question = null!;
            }
        }
    }

    /// <summary>
    /// Process memory records for ASK and SEARCH calls
    /// </summary>
    /// <param name="result">Current state of the result</param>
    /// <param name="record">Memory record, e.g. text chunk + metadata</param>
    /// <param name="recordRelevance">Memory record relevance</param>
    /// <param name="index">Memory index name</param>
    /// <param name="includeDupes">Whether to include or skip duplicate chunks of text</param>
    /// <param name="factTemplate">How to render the record when preparing an LLM prompt</param>
    /// <returns>Updated search result state</returns>
    private SearchClientResult ProcessMemoryRecord(
        SearchClientResult result, string index, MemoryRecord record, double recordRelevance, bool includeDupes = true, string? factTemplate = null)
    {
        var partitionText = record.GetPartitionText(this._log).Trim();
        if (string.IsNullOrEmpty(partitionText))
        {
            this._log.LogError("The document partition is empty, doc: {0}", record.Id);
            return result.SkipRecord();
        }

        // Keep track of how many records have been processed
        result.RecordCount++;

        // Note: a document can be composed by multiple files
        string documentId = record.GetDocumentId(this._log);

        // Identify the file in case there are multiple files
        string fileId = record.GetFileId(this._log);

        // Note: this is not a URL and perhaps could be dropped. For now it acts as a unique identifier. See also SourceUrl.
        string linkToFile = $"{index}/{documentId}/{fileId}";

        // Note: this is "content.url" when importing web pages
        string fileName = record.GetFileName(this._log);

        // Link to the web page (if a web page) or link to KM web endpoint to download the file
        string fileDownloadUrl = record.GetWebPageUrl(index);

        // Name of the file to show to the LLM, avoiding "content.url"
        string fileNameForLLM = (fileName == "content.url" ? fileDownloadUrl : fileName);

        // Dupes management note: don't skip the record, only skip the chunk in the prompt
        // so Citations includes also duplicates, which might have different tags
        bool isDupe = !result.FactsUniqueness.Add($"{partitionText}");
        bool skipFactInPrompt = (isDupe && !includeDupes);

        if (result.Mode == SearchMode.SearchMode)
        {
            // Relevance is `float.MinValue` when search uses only filters
            if (recordRelevance > float.MinValue) { this._log.LogTrace("Adding result with relevance {0}", recordRelevance); }
        }
        else if (result.Mode == SearchMode.AskMode)
        {
            result.FactsAvailableCount++;

            if (!skipFactInPrompt)
            {
                string fact = PromptUtils.RenderFactTemplate(
                    template: factTemplate!,
                    factContent: partitionText,
                    source: fileNameForLLM,
                    relevance: recordRelevance.ToString("P1", CultureInfo.CurrentCulture),
                    recordId: record.Id,
                    tags: record.Tags,
                    metadata: record.Payload);

                // Use the partition/chunk only if there's room for it
                int factSizeInTokens = this._textGenerator.CountTokens(fact);
                if (factSizeInTokens >= result.TokensAvailable)
                {
                    // Stop after reaching the max number of tokens
                    return result.Stop();
                }

                result.Facts.Append(fact);
                result.FactsUsedCount++;
                result.TokensAvailable -= factSizeInTokens;

                // Relevance is cosine similarity when not using hybrid search
                this._log.LogTrace("Adding content #{FactsUsedCount} with relevance {Relevance} (dupe: {IsDupe})",
                    result.FactsUsedCount, recordRelevance, isDupe);
            }
            else
            {
                // The counter must be increased to avoid long/infinite loops
                // in case the storage contains several duplications
                result.FactsUsedCount++;
            }
        }

        var citation = result.Mode switch
        {
            SearchMode.SearchMode => result.SearchResult.Results.FirstOrDefault(x => x.Link == linkToFile),
            SearchMode.AskMode => result.AskResult.RelevantSources.FirstOrDefault(x => x.Link == linkToFile),
            _ => throw new ArgumentOutOfRangeException(nameof(result.Mode))
        };

        if (citation == null)
        {
            citation = new Citation();
            result.AddSource(citation);
        }

        citation.Index = index;
        citation.DocumentId = documentId;
        citation.FileId = fileId;
        citation.Link = linkToFile;
        citation.SourceContentType = record.GetFileContentType(this._log);
        citation.SourceName = fileName;
        citation.SourceUrl = fileDownloadUrl;
        citation.Partitions.Add(new Citation.Partition
        {
            Text = partitionText,
            Relevance = (float)recordRelevance,
            PartitionNumber = record.GetPartitionNumber(this._log),
            SectionNumber = record.GetSectionNumber(),
            LastUpdate = record.GetLastUpdate(),
            Tags = record.Tags,
        });

        // Stop when reaching the max number of results or facts. This acts also as
        // a protection against storage connectors disregarding 'limit' and returning too many records.
        if ((result.Mode == SearchMode.SearchMode && result.SearchResult.Results.Count >= result.MaxRecordCount)
            || (result.Mode == SearchMode.AskMode && result.FactsUsedCount >= result.MaxRecordCount))
        {
            return result.Stop();
        }

        return result;
    }
}
