// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
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

#pragma warning disable CA2254
[Experimental("KMEXP05")]
public sealed class SearchClient : ISearchClient
{
    private readonly IMemoryDb _memoryDb;
    private readonly ITextGenerator _textGenerator;
    private readonly IContentModeration? _contentModeration;
    private readonly SearchClientConfig _config;
    private readonly ILogger<SearchClient> _log;
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
        this._contentModeration = contentModeration;
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

        this._log.LogTrace(string.IsNullOrEmpty(query)
            ? $"Fetching relevant memories by similarity, min relevance {minRelevance}"
            : "Fetching relevant memories by filtering only, no vector search");

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
        string emptyAnswer = context.GetCustomEmptyAnswerTextOrDefault(this._config.EmptyAnswer);
        string answerPrompt = context.GetCustomRagPromptOrDefault(this._answerPrompt);
        int limit = context.GetCustomRagMaxMatchesCountOrDefault(this._config.MaxMatchesCount);

        var maxTokens = this._config.MaxAskPromptSize > 0
            ? this._config.MaxAskPromptSize
            : this._textGenerator.MaxTokenTotal;

        SearchClientResult result = SearchClientResult.AskResultInstance(
            question: question,
            emptyAnswer: emptyAnswer,
            maxGroundingFacts: limit,
            tokensAvailable: maxTokens
                             - this._textGenerator.CountTokens(answerPrompt)
                             - this._textGenerator.CountTokens(question)
                             - this._config.AnswerTokens
        );

        if (string.IsNullOrEmpty(question))
        {
            this._log.LogWarning("No question provided");
            return result.AskResult;
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
            result = this.ProcessMemoryRecord(result, index, memoryRecord, recordRelevance, factTemplate);

            if (result.State == SearchState.SkipRecord) { continue; }

            if (result.State == SearchState.Stop) { break; }
        }

        return await this.GenerateAnswerAsync(question, result, minRelevance, context, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Process memory records for ASK and SEARCH calls
    /// </summary>
    /// <param name="state">Current state of the result</param>
    /// <param name="record">Memory record, e.g. text chunk + metadata</param>
    /// <param name="recordRelevance">Memory record relevance</param>
    /// <param name="index">Memory index name</param>
    /// <param name="factTemplate">How to render the record when preparing an LLM prompt</param>
    /// <returns>Updated result state</returns>
    private SearchClientResult ProcessMemoryRecord(
        SearchClientResult state, string index, MemoryRecord record, double recordRelevance, string? factTemplate = null)
    {
        var partitionText = record.GetPartitionText(this._log).Trim();
        if (string.IsNullOrEmpty(partitionText))
        {
            this._log.LogError("The document partition is empty, doc: {0}", record.Id);
            return state.SkipRecord();
        }

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

        if (state.Mode == SearchMode.SearchMode)
        {
            // Relevance is `float.MinValue` when search uses only filters
            if (recordRelevance > float.MinValue) { this._log.LogTrace("Adding result with relevance {0}", recordRelevance); }
        }
        else if (state.Mode == SearchMode.AskMode)
        {
            state.FactsAvailableCount++;

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
            if (factSizeInTokens >= state.TokensAvailable)
            {
                // Stop after reaching the max number of tokens
                return state.Stop();
            }

            state.Facts.Append(fact);
            state.FactsUsedCount++;
            state.TokensAvailable -= factSizeInTokens;

            // Relevance is cosine similarity when not using hybrid search
            this._log.LogTrace("Adding content #{0} with relevance {1}", state.FactsUsedCount, recordRelevance);
        }

        Citation? citation;
        if (state.Mode == SearchMode.SearchMode)
        {
            citation = state.SearchResult.Results.FirstOrDefault(x => x.Link == linkToFile);
            if (citation == null)
            {
                citation = new Citation();
                state.SearchResult.Results.Add(citation);
            }
        }
        else if (state.Mode == SearchMode.AskMode)
        {
            // If the file is already in the list of citations, only add the partition
            citation = state.AskResult.RelevantSources.FirstOrDefault(x => x.Link == linkToFile);
            if (citation == null)
            {
                citation = new Citation();
                state.AskResult.RelevantSources.Add(citation);
            }
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(state.Mode));
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

        if (state.Mode == SearchMode.SearchMode)
        {
            if (state.SearchResult.Results.Count >= state.MaxRecordCount)
            {
                return state.Stop();
            }
        }
        else if (state.Mode == SearchMode.AskMode)
        {
            // In cases where a buggy storage connector is returning too many records
            if (state.FactsUsedCount >= state.MaxRecordCount)
            {
                return state.Stop();
            }
        }

        return state;
    }

    private async Task<MemoryAnswer> GenerateAnswerAsync(
        string question, SearchClientResult result, double minRelevance, IContext? context, CancellationToken cancellationToken)
    {
        if (result.FactsAvailableCount > 0 && result.FactsUsedCount == 0)
        {
            this._log.LogError("Unable to inject memories in the prompt, not enough tokens available");
            result.AskResult.NoResultReason = "Unable to use memories";
            return result.AskResult;
        }

        if (result.FactsUsedCount == 0)
        {
            this._log.LogWarning("No memories available (min relevance: {0})", minRelevance);
            result.AskResult.NoResultReason = "No memories available";
            return result.AskResult;
        }

        // Collect the LLM output
        var text = new StringBuilder();
        var charsGenerated = 0;
        var watch = new Stopwatch();
        watch.Restart();
        await foreach (var x in this.GenerateAnswerTokensAsync(question, result.Facts.ToString(), context, cancellationToken).ConfigureAwait(false))
        {
            text.Append(x);

            if (this._log.IsEnabled(LogLevel.Trace) && text.Length - charsGenerated >= 30)
            {
                charsGenerated = text.Length;
                this._log.LogTrace("{0} chars generated", charsGenerated);
            }
        }

        watch.Stop();

        // Finalize the answer, checking if it's empty
        result.AskResult.Result = text.ToString();
        this._log.LogSensitive("Answer: {0}", result.AskResult.Result);
        result.AskResult.NoResult = ValueIsEquivalentTo(result.AskResult.Result, this._config.EmptyAnswer);
        if (result.AskResult.NoResult)
        {
            result.AskResult.NoResultReason = "No relevant memories found";
            this._log.LogTrace("Answer generated in {0} msecs. No relevant memories found", watch.ElapsedMilliseconds);
        }
        else
        {
            this._log.LogTrace("Answer generated in {0} msecs", watch.ElapsedMilliseconds);
        }

        // Validate the LLM output
        if (this._contentModeration != null && this._config.UseContentModeration)
        {
            var isSafe = await this._contentModeration.IsSafeAsync(result.AskResult.Result, cancellationToken).ConfigureAwait(false);
            if (!isSafe)
            {
                this._log.LogWarning("Unsafe answer detected. Returning error message instead.");
                this._log.LogSensitive("Unsafe answer: {0}", result.AskResult.Result);
                result.AskResult.NoResultReason = "Content moderation failure";
                result.AskResult.Result = this._config.ModeratedAnswer;
            }
        }

        return result.AskResult;
    }

    private IAsyncEnumerable<string> GenerateAnswerTokensAsync(string question, string facts, IContext? context, CancellationToken cancellationToken)
    {
        string prompt = context.GetCustomRagPromptOrDefault(this._answerPrompt);
        int maxTokens = context.GetCustomRagMaxTokensOrDefault(this._config.AnswerTokens);
        double temperature = context.GetCustomRagTemperatureOrDefault(this._config.Temperature);
        double nucleusSampling = context.GetCustomRagNucleusSamplingOrDefault(this._config.TopP);

        prompt = prompt.Replace("{{$facts}}", facts.Trim(), StringComparison.OrdinalIgnoreCase);

        question = question.Trim();
        question = question.EndsWith('?') ? question : $"{question}?";
        prompt = prompt.Replace("{{$input}}", question, StringComparison.OrdinalIgnoreCase);
        prompt = prompt.Replace("{{$notFound}}", this._config.EmptyAnswer, StringComparison.OrdinalIgnoreCase);

        var options = new TextGenerationOptions
        {
            MaxTokens = maxTokens,
            Temperature = temperature,
            NucleusSampling = nucleusSampling,
            PresencePenalty = this._config.PresencePenalty,
            FrequencyPenalty = this._config.FrequencyPenalty,
            StopSequences = this._config.StopSequences,
            TokenSelectionBiases = this._config.TokenSelectionBiases,
        };

        if (this._log.IsEnabled(LogLevel.Debug))
        {
            this._log.LogDebug("Running RAG prompt, size: {0} tokens, requesting max {1} tokens",
                this._textGenerator.CountTokens(prompt),
                this._config.AnswerTokens);

            this._log.LogSensitive("Prompt: {0}", prompt);
        }

        return this._textGenerator.GenerateTextAsync(prompt, options, cancellationToken);
    }

    private static bool ValueIsEquivalentTo(string value, string target)
    {
        value = value.Trim().Trim('.', '"', '\'', '`', '~', '!', '?', '@', '#', '$', '%', '^', '+', '*', '_', '-', '=', '|', '\\', '/', '(', ')', '[', ']', '{', '}', '<', '>');
        target = target.Trim().Trim('.', '"', '\'', '`', '~', '!', '?', '@', '#', '$', '%', '^', '+', '*', '_', '-', '=', '|', '\\', '/', '(', ')', '[', ']', '{', '}', '<', '>');
        return string.Equals(value, target, StringComparison.OrdinalIgnoreCase);
    }
}
