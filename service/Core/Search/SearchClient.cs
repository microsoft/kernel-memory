// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.Prompts;

namespace Microsoft.KernelMemory.Search;

public class SearchClient : ISearchClient
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
            if (!memory.Tags.ContainsKey(Constants.ReservedDocumentIdTag))
            {
                this._log.LogError("The memory record is missing the '{0}' tag", Constants.ReservedDocumentIdTag);
            }

            if (!memory.Tags.ContainsKey(Constants.ReservedFileIdTag))
            {
                this._log.LogError("The memory record is missing the '{0}' tag", Constants.ReservedFileIdTag);
            }

            if (!memory.Tags.ContainsKey(Constants.ReservedFileTypeTag))
            {
                this._log.LogError("The memory record is missing the '{0}' tag", Constants.ReservedFileTypeTag);
            }

            // Note: a document can be composed by multiple files
            string documentId = memory.Tags[Constants.ReservedDocumentIdTag].FirstOrDefault() ?? string.Empty;

            // Identify the file in case there are multiple files
            string fileId = memory.Tags[Constants.ReservedFileIdTag].FirstOrDefault() ?? string.Empty;

            // TODO: URL to access the file
            string linkToFile = $"{documentId}/{fileId}";

            string fileContentType = memory.Tags[Constants.ReservedFileTypeTag].FirstOrDefault() ?? string.Empty;
            string fileName = memory.Payload[Constants.ReservedPayloadFileNameField].ToString() ?? string.Empty;

            var partitionText = memory.Payload[Constants.ReservedPayloadTextField].ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(partitionText))
            {
                this._log.LogError("The document partition is empty, doc: {0}", memory.Id);
                continue;
            }

            if (relevance > float.MinValue)
            {
                this._log.LogTrace("Adding result with relevance {0}", relevance);
            }

            // If the file is already in the list of citations, only add the partition
            var citation = result.Results.FirstOrDefault(x => x.Link == linkToFile);
            if (citation == null)
            {
                citation = new Citation();
                result.Results.Add(citation);
            }

            // Add the partition to the list of citations
            citation.Link = linkToFile;
            citation.SourceContentType = fileContentType;
            citation.SourceName = fileName;
            citation.Tags = memory.Tags;

#pragma warning disable CA1806 // it's ok if parsing fails
            DateTimeOffset.TryParse(memory.Payload[Constants.ReservedPayloadLastUpdateField].ToString(), out var lastUpdate);
#pragma warning restore CA1806

            citation.Partitions.Add(new Citation.Partition
            {
                Text = partitionText,
                Relevance = (float)relevance,
                LastUpdate = lastUpdate,
            });
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
        if (string.IsNullOrEmpty(question))
        {
            this._log.LogWarning("No question provided");
            return new MemoryAnswer
            {
                Question = question,
                Result = this._config.EmptyAnswer,
            };
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

        var answer = new MemoryAnswer
        {
            Question = question,
            Result = this._config.EmptyAnswer,
        };

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
            if (!memory.Tags.ContainsKey(Constants.ReservedDocumentIdTag))
            {
                this._log.LogError("The memory record is missing the '{0}' tag", Constants.ReservedDocumentIdTag);
            }

            if (!memory.Tags.ContainsKey(Constants.ReservedFileIdTag))
            {
                this._log.LogError("The memory record is missing the '{0}' tag", Constants.ReservedFileIdTag);
            }

            if (!memory.Tags.ContainsKey(Constants.ReservedFileTypeTag))
            {
                this._log.LogError("The memory record is missing the '{0}' tag", Constants.ReservedFileTypeTag);
            }

            // Note: a document can be composed by multiple files
            string documentId = memory.Tags[Constants.ReservedDocumentIdTag].FirstOrDefault() ?? string.Empty;

            // Identify the file in case there are multiple files
            string fileId = memory.Tags[Constants.ReservedFileIdTag].FirstOrDefault() ?? string.Empty;

            // TODO: URL to access the file
            string linkToFile = $"{documentId}/{fileId}";

            string fileContentType = memory.Tags[Constants.ReservedFileTypeTag].FirstOrDefault() ?? string.Empty;
            string fileName = memory.Payload[Constants.ReservedPayloadFileNameField].ToString() ?? string.Empty;

            factsAvailableCount++;
            var partitionText = memory.Payload[Constants.ReservedPayloadTextField].ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(partitionText))
            {
                this._log.LogError("The document partition is empty, doc: {0}", memory.Id);
                continue;
            }

            // TODO: add file age in days, to push relevance of newer documents
            var fact = $"==== [File:{fileName};Relevance:{relevance:P1}]:\n{partitionText}\n";

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

            // If the file is already in the list of citations, only add the partition
            var citation = answer.RelevantSources.FirstOrDefault(x => x.Link == linkToFile);
            if (citation == null)
            {
                citation = new Citation();
                answer.RelevantSources.Add(citation);
            }

            // Add the partition to the list of citations
            citation.Link = linkToFile;
            citation.SourceContentType = fileContentType;
            citation.SourceName = fileName;
            citation.Tags = memory.Tags;

#pragma warning disable CA1806 // it's ok if parsing fails
            DateTimeOffset.TryParse(memory.Payload[Constants.ReservedPayloadLastUpdateField].ToString(), out var lastUpdate);
#pragma warning restore CA1806

            citation.Partitions.Add(new Citation.Partition
            {
                Text = partitionText,
                Relevance = (float)relevance,
                LastUpdate = lastUpdate,
            });
        }

        if (factsAvailableCount > 0 && factsUsedCount == 0)
        {
            this._log.LogError("Unable to inject memories in the prompt, not enough tokens available");
            return new MemoryAnswer { Question = question, Result = "INFO NOT FOUND" };
        }

        if (factsUsedCount == 0)
        {
            this._log.LogWarning("No memories available");
            return new MemoryAnswer { Question = question, Result = "INFO NOT FOUND" };
        }

        var text = new StringBuilder();
        var charsGenerated = 0;
        var watch = new Stopwatch();
        watch.Restart();
        await foreach (var x in this.GenerateAnswerAsync(question, facts.ToString())
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
        this._log.LogTrace("Answer generated in {0} msecs", watch.ElapsedMilliseconds);

        answer.Result = text.ToString();

        return answer;
    }

    private IAsyncEnumerable<string> GenerateAnswerAsync(string question, string facts)
    {
        var prompt = this._answerPrompt;
        prompt = prompt.Replace("{{$facts}}", facts.Trim(), StringComparison.OrdinalIgnoreCase);

        question = question.Trim();
        question = question.EndsWith('?') ? question : $"{question}?";
        prompt = prompt.Replace("{{$input}}", question, StringComparison.OrdinalIgnoreCase);

        // TODO: receive options from API: https://github.com/microsoft/kernel-memory/issues/137
        var options = new TextGenerationOptions
        {
            // Temperature = 0,
            // TopP = 0,
            // PresencePenalty = 0,
            // FrequencyPenalty = 0,
            MaxTokens = this._config.AnswerTokens,
            // StopSequences = null,
            // TokenSelectionBiases = null
        };

        if (this._log.IsEnabled(LogLevel.Debug))
        {
            this._log.LogDebug("Running RAG prompt, size: {0} tokens, requesting max {1} tokens",
                this._textGenerator.CountTokens(prompt),
                this._config.AnswerTokens);
        }

        return this._textGenerator.GenerateTextAsync(prompt, options);
    }
}
