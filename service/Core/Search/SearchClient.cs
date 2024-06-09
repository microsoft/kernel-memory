// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.Prompts;

namespace Microsoft.KernelMemory.Search;

internal sealed class SearchClient : ISearchClient
{
    private readonly IMemoryDb _memoryDb;
    private readonly ITextGenerator _textGenerator;
    private readonly SearchClientConfig _config;
    private readonly ILogger<SearchClient> _log;
    private readonly string _answerPrompt;

    public SearchClient(
        IMemoryDb memoryDb,
        ITextGenerator textGenerator,
        SearchClientConfig? config = null,
        IPromptProvider? promptProvider = null,
        ILogger<SearchClient>? log = null)
    {
        this._memoryDb = memoryDb;
        this._textGenerator = textGenerator;
        this._config = config ?? new SearchClientConfig();
        this._config.Validate();

        promptProvider ??= new EmbeddedPromptProvider();
        this._answerPrompt = promptProvider.ReadPrompt(Constants.PromptNamesAnswerWithFacts);

        this._log = log ?? DefaultLogger<SearchClient>.Instance;

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
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0) { limit = this._config.MaxMatchesCount; }

        var result = new SearchResult
        {
            Query = query,
            Results = new List<Citation>()
        };

        if (string.IsNullOrWhiteSpace(query) && (filters == null || filters.Count == 0))
        {
            this._log.LogWarning("No query or filters provided");
            return result;
        }

        var list = new List<(MemoryRecord memory, double relevance)>();
        if (!string.IsNullOrEmpty(query))
        {
            this._log.LogTrace("Fetching relevant memories by similarity, min relevance {0}", minRelevance);
            IAsyncEnumerable<(MemoryRecord, double)> matches = this._memoryDb.GetSimilarListAsync(
                index: index,
                text: query,
                filters: filters,
                minRelevance: minRelevance,
                limit: limit,
                withEmbeddings: false,
                cancellationToken: cancellationToken);

            // Memories are sorted by relevance, starting from the most relevant
            await foreach ((MemoryRecord memory, double relevance) in matches.ConfigureAwait(false))
            {
                list.Add((memory, relevance));
            }
        }
        else
        {
            this._log.LogTrace("Fetching relevant memories by filtering");
            IAsyncEnumerable<MemoryRecord> matches = this._memoryDb.GetListAsync(
                index: index,
                filters: filters,
                limit: limit,
                withEmbeddings: false,
                cancellationToken: cancellationToken);

            await foreach (MemoryRecord memory in matches.ConfigureAwait(false))
            {
                list.Add((memory, float.MinValue));
            }
        }

        // Memories are sorted by relevance, starting from the most relevant
        foreach ((MemoryRecord memory, double relevance) in list)
        {
            var partitionText = memory.GetPartitionText(this._log).Trim();
            if (string.IsNullOrEmpty(partitionText))
            {
                this._log.LogError("The document partition is empty, doc: {0}", memory.Id);
                continue;
            }

            // Relevance is `float.MinValue` when search uses only filters and no embeddings (see code above)
            if (relevance > float.MinValue) { this._log.LogTrace("Adding result with relevance {0}", relevance); }

            this.MapMatchToCitation(index, result.Results, memory, relevance);

            // In cases where a buggy storage connector is returning too many records
            if (result.Results.Count >= this._config.MaxMatchesCount)
            {
                break;
            }
        }

        if (result.Results.Count == 0)
        {
            this._log.LogDebug("No memories found");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<MemoryAnswer> AskAsync(
        string index,
        string question,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        CancellationToken cancellationToken = default)
    {
        var noAnswerFound = new MemoryAnswer
        {
            Question = question,
            NoResult = true,
            Result = this._config.EmptyAnswer,
        };

        if (string.IsNullOrEmpty(question))
        {
            this._log.LogWarning("No question provided");
            noAnswerFound.NoResultReason = "No question provided";
            return noAnswerFound;
        }

        var facts = new StringBuilder();
        var maxTokens = this._config.MaxAskPromptSize > 0
            ? this._config.MaxAskPromptSize
            : this._textGenerator.MaxTokenTotal;
        var tokensAvailable = maxTokens
                              - this._textGenerator.CountTokens(this._answerPrompt)
                              - this._textGenerator.CountTokens(question)
                              - this._config.AnswerTokens;

        var factsUsedCount = 0;
        var factsAvailableCount = 0;
        var answer = noAnswerFound;

        this._log.LogTrace("Fetching relevant memories");
        IAsyncEnumerable<(MemoryRecord, double)> matches = this._memoryDb.GetSimilarListAsync(
            index: index,
            text: question,
            filters: filters,
            minRelevance: minRelevance,
            limit: this._config.MaxMatchesCount,
            withEmbeddings: false,
            cancellationToken: cancellationToken);

        // Memories are sorted by relevance, starting from the most relevant
        await foreach ((MemoryRecord memory, double relevance) in matches.ConfigureAwait(false))
        {
            string fileName = memory.GetFileName(this._log);
            string webPageUrl = memory.GetWebPageUrl(index);

            var partitionText = memory.GetPartitionText(this._log).Trim();
            if (string.IsNullOrEmpty(partitionText))
            {
                this._log.LogError("The document partition is empty, doc: {0}", memory.Id);
                continue;
            }

            factsAvailableCount++;

            var fact = GenerateFactString(fileName, webPageUrl, relevance, partitionText);

            // Use the partition/chunk only if there's room for it
            var size = this._textGenerator.CountTokens(fact);
            if (size >= tokensAvailable)
            {
                // Stop after reaching the max number of tokens
                break;
            }

            factsUsedCount++;
            this._log.LogTrace("Adding text {0} with relevance {1}", factsUsedCount, relevance);

            facts.Append(fact);
            tokensAvailable -= size;

            this.MapMatchToCitation(index, answer.RelevantSources, memory, relevance);

            // In cases where a buggy storage connector is returning too many records
            if (factsUsedCount >= this._config.MaxMatchesCount)
            {
                break;
            }
        }

        if (factsAvailableCount > 0 && factsUsedCount == 0)
        {
            this._log.LogError("Unable to inject memories in the prompt, not enough tokens available");
            noAnswerFound.NoResultReason = "Unable to use memories";
            return noAnswerFound;
        }

        if (factsUsedCount == 0)
        {
            this._log.LogWarning("No memories available");
            noAnswerFound.NoResultReason = "No memories available";
            return noAnswerFound;
        }

        var text = new StringBuilder();
        var charsGenerated = 0;
        var watch = new Stopwatch();
        watch.Restart();
        await foreach (var x in this.GenerateAnswer(question, facts.ToString())
                           .WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            text.Append(x);

            if (this._log.IsEnabled(LogLevel.Trace) && text.Length - charsGenerated >= 30)
            {
                charsGenerated = text.Length;
                this._log.LogTrace("{0} chars generated", charsGenerated);
            }
        }

        watch.Stop();

        answer.Result = text.ToString();
        var noResult = ValueIsEquivalentTo(answer.Result, this._config.EmptyAnswer);
        answer.NoResult = noResult;
        if (noResult)
        {
            answer.NoResultReason = "No relevant memories found";
            this._log.LogTrace("Answer generated in {0} msecs. No relevant memories found", watch.ElapsedMilliseconds);
        }
        else
        {
            this._log.LogTrace("Answer generated in {0} msecs", watch.ElapsedMilliseconds);
        }

        return answer;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<MemoryAnswer> AskStreamingAsync(
        string index,
        string question,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var noAnswerFound = new MemoryAnswer
        {
            Question = question,
            NoResult = true,
            Result = this._config.EmptyAnswer,
        };

        if (string.IsNullOrEmpty(question))
        {
            this._log.LogWarning("No question provided");
            noAnswerFound.NoResultReason = "No question provided";
            yield return noAnswerFound;
            yield break;
        }

        var facts = new StringBuilder();
        var maxTokens = this._config.MaxAskPromptSize > 0
            ? this._config.MaxAskPromptSize
            : this._textGenerator.MaxTokenTotal;
        var tokensAvailable = maxTokens
                              - this._textGenerator.CountTokens(this._answerPrompt)
                              - this._textGenerator.CountTokens(question)
                              - this._config.AnswerTokens;

        var factsUsedCount = 0;
        var factsAvailableCount = 0;
        var answer = noAnswerFound;

        this._log.LogTrace("Fetching relevant memories");
        IAsyncEnumerable<(MemoryRecord, double)> matches = this._memoryDb.GetSimilarListAsync(
            index: index,
            text: question,
            filters: filters,
            minRelevance: minRelevance,
            limit: this._config.MaxMatchesCount,
            withEmbeddings: false,
            cancellationToken: cancellationToken);

        // Memories are sorted by relevance, starting from the most relevant
        await foreach ((MemoryRecord memory, double relevance) in matches.ConfigureAwait(false))
        {
            string fileName = memory.GetFileName(this._log);
            string webPageUrl = memory.GetWebPageUrl(index);

            var partitionText = memory.GetPartitionText(this._log).Trim();
            if (string.IsNullOrEmpty(partitionText))
            {
                this._log.LogError("The document partition is empty, doc: {0}", memory.Id);
                continue;
            }

            factsAvailableCount++;

            var fact = GenerateFactString(fileName, webPageUrl, relevance, partitionText);

            // Use the partition/chunk only if there's room for it
            var size = this._textGenerator.CountTokens(fact);
            if (size >= tokensAvailable)
            {
                // Stop after reaching the max number of tokens
                break;
            }

            factsUsedCount++;
            this._log.LogTrace("Adding text {0} with relevance {1}", factsUsedCount, relevance);

            facts.Append(fact);
            tokensAvailable -= size;

            this.MapMatchToCitation(index, answer.RelevantSources, memory, relevance);

            // In cases where a buggy storage connector is returning too many records
            if (factsUsedCount >= this._config.MaxMatchesCount)
            {
                break;
            }
        }

        if (factsAvailableCount > 0 && factsUsedCount == 0)
        {
            this._log.LogError("Unable to inject memories in the prompt, not enough tokens available");
            noAnswerFound.NoResultReason = "Unable to use memories";
            yield return noAnswerFound;
            yield break;
        }

        if (factsUsedCount == 0)
        {
            this._log.LogWarning("No memories available");
            noAnswerFound.NoResultReason = "No memories available";
            yield return noAnswerFound;
            yield break;
        }

        StringBuilder bufferedAnswer = new();
        bool finishedRequiredBuffering = false;
        var watch = Stopwatch.StartNew();
        await foreach (var token in this.GenerateAnswer(question, facts.ToString()).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (token is null || token.Length == 0)
            {
                continue;
            }

            bufferedAnswer.Append(token);

            int currentLength = bufferedAnswer.Length;

            if (!finishedRequiredBuffering)
            {
                // Adding 5 to the length to ensure that the extra tokens in ValueIsEquivalentTo can be checked (non-text tokens)
                if (currentLength <= this._config.EmptyAnswer.Length + 5 && ValueIsEquivalentTo(bufferedAnswer.ToString(), this._config.EmptyAnswer))
                {
                    this._log.LogTrace("Answer generated in {0} msecs. No relevant memories found", watch.ElapsedMilliseconds);
                    noAnswerFound.NoResultReason = "No relevant memories found";
                    yield return noAnswerFound;
                    yield break;
                }
                else if (currentLength > this._config.EmptyAnswer.Length)
                {
                    finishedRequiredBuffering = true;
                    answer.NoResult = false;
                    answer.Result = bufferedAnswer.ToString();
                    yield return answer;
                }
            }
            else
            {
                yield return new MemoryAnswer
                {
                    Result = token,
                    NoResult = false,
                    Question = "",
                    RelevantSources = [],
                };
            }

            if (this._log.IsEnabled(LogLevel.Trace) && currentLength >= 30)
            {
                this._log.LogTrace("{0} chars generated", currentLength);
            }
        }

        //Edge case when the generated answer is shorter than the configured empty answer
        if (!finishedRequiredBuffering)
        {
            answer.NoResult = false;
            answer.Result = bufferedAnswer.ToString();
            yield return answer;
        }

        watch.Stop();
        this._log.LogTrace("Answer generated in {0} msecs", watch.ElapsedMilliseconds);
    }

    private IAsyncEnumerable<string> GenerateAnswer(string question, string facts)
    {
        var prompt = this._answerPrompt;
        prompt = prompt.Replace("{{$facts}}", facts.Trim(), StringComparison.OrdinalIgnoreCase);

        question = question.Trim();
        question = question.EndsWith('?') ? question : $"{question}?";
        prompt = prompt.Replace("{{$input}}", question, StringComparison.OrdinalIgnoreCase);

        prompt = prompt.Replace("{{$notFound}}", this._config.EmptyAnswer, StringComparison.OrdinalIgnoreCase);

        var options = new TextGenerationOptions
        {
            Temperature = this._config.Temperature,
            TopP = this._config.TopP,
            PresencePenalty = this._config.PresencePenalty,
            FrequencyPenalty = this._config.FrequencyPenalty,
            MaxTokens = this._config.AnswerTokens,
            StopSequences = this._config.StopSequences,
            TokenSelectionBiases = this._config.TokenSelectionBiases,
        };

        if (this._log.IsEnabled(LogLevel.Debug))
        {
            this._log.LogDebug("Running RAG prompt, size: {0} tokens, requesting max {1} tokens",
                this._textGenerator.CountTokens(prompt),
                this._config.AnswerTokens);
        }

        return this._textGenerator.GenerateTextAsync(prompt, options);
    }

    private static bool ValueIsEquivalentTo(string value, string target)
    {
        value = value.Trim().Trim('.', '"', '\'', '`', '~', '!', '?', '@', '#', '$', '%', '^', '+', '*', '_', '-', '=', '|', '\\', '/', '(', ')', '[', ']', '{', '}', '<', '>');
        target = target.Trim().Trim('.', '"', '\'', '`', '~', '!', '?', '@', '#', '$', '%', '^', '+', '*', '_', '-', '=', '|', '\\', '/', '(', ')', '[', ']', '{', '}', '<', '>');
        return string.Equals(value, target, StringComparison.OrdinalIgnoreCase);
    }

    private static string GenerateFactString(string fileName, string webPageUrl, double relevance, string partitionText)
    {
        // TODO: add file age in days, to push relevance of newer documents
        return $"==== [File:{(fileName == "content.url" ? webPageUrl : fileName)};Relevance:{relevance:P1}]:\n{partitionText}\n";
    }

    private void MapMatchToCitation(string index, List<Citation> citations, MemoryRecord memory, double relevance)
    {
        string partitionText = memory.GetPartitionText(this._log).Trim();

        // Note: a document can be composed by multiple files
        string documentId = memory.GetDocumentId(this._log);

        // Identify the file in case there are multiple files
        string fileId = memory.GetFileId(this._log);

        // Note: this is not a URL and perhaps could be dropped. For now it acts as a unique identifier. See also SourceUrl.
        string linkToFile = $"{index}/{documentId}/{fileId}";

        // If the file is already in the list of citations, only add the partition
        Citation? citation = citations.Find(x => x.Link == linkToFile);
        if (citation == null)
        {
            citation = new Citation();
            citations.Add(citation);
        }

        // Add the partition to the list of citations
        citation.Index = index;
        citation.DocumentId = documentId;
        citation.FileId = fileId;
        citation.Link = linkToFile;
        citation.SourceContentType = memory.GetFileContentType(this._log);
        citation.SourceName = memory.GetFileName(this._log);
        citation.SourceUrl = memory.GetWebPageUrl(index);

        citation.Partitions.Add(new Citation.Partition
        {
            Text = partitionText,
            Relevance = (float)relevance,
            PartitionNumber = memory.GetPartitionNumber(this._log),
            SectionNumber = memory.GetSectionNumber(),
            LastUpdate = memory.GetLastUpdate(),
            Tags = memory.Tags,
        });
    }
}
