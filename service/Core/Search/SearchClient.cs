// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.Tokenizers.GPT3;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.Prompts;

namespace Microsoft.KernelMemory.Search;

public class SearchClient
{
    private const int MaxMatchesCount = 100;
    private const int AnswerTokens = 300;

    private readonly IMemoryDb _memoryDb;
    private readonly ITextGeneration _textGenerator;
    private readonly ILogger<SearchClient> _log;
    private readonly string _answerPrompt;

    public SearchClient(
        IMemoryDb memoryDb,
        ITextGeneration textGenerator,
        IPromptProvider? promptProvider = null,
        ILogger<SearchClient>? log = null)
    {
        this._memoryDb = memoryDb;
        this._textGenerator = textGenerator;

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

    public Task<IEnumerable<string>> ListIndexesAsync(CancellationToken cancellationToken = default)
    {
        return this._memoryDb.GetIndexesAsync(cancellationToken);
    }

    public async Task<SearchResult> SearchAsync(
        string index,
        string query,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        int limit = -1,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0) { limit = MaxMatchesCount; }

        var result = new SearchResult
        {
            Query = query,
            Results = new List<Citation>()
        };

        if (string.IsNullOrEmpty(query))
        {
            this._log.LogWarning("No query provided");
            return result;
        }

        this._log.LogTrace("Fetching relevant memories");
        IAsyncEnumerable<(MemoryRecord, double)> matches = this._memoryDb.GetSimilarListAsync(
            index: index,
            text: query,
            filters: filters,
            minRelevance: minRelevance,
            limit: limit,
            withEmbeddings: false,
            cancellationToken: cancellationToken);

        // Memories are sorted by relevance, starting from the most relevant
        await foreach ((MemoryRecord memory, double relevance) in matches.WithCancellation(cancellationToken).ConfigureAwait(false))
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

            this._log.LogTrace("Adding result with relevance {0}", relevance);

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
                Result = "INFO NOT FOUND",
            };
        }

        var facts = new StringBuilder();
        var tokensAvailable = 8000
                              - GPT3Tokenizer.Encode(this._answerPrompt).Count
                              - GPT3Tokenizer.Encode(question).Count
                              - AnswerTokens;

        var factsUsedCount = 0;
        var factsAvailableCount = 0;

        var answer = new MemoryAnswer
        {
            Question = question,
            Result = "INFO NOT FOUND",
        };

        this._log.LogTrace("Fetching relevant memories");
        IAsyncEnumerable<(MemoryRecord, double)> matches = this._memoryDb.GetSimilarListAsync(
            index: index,
            text: question,
            filters: filters,
            minRelevance: minRelevance,
            limit: MaxMatchesCount,
            withEmbeddings: false,
            cancellationToken: cancellationToken);

        // Memories are sorted by relevance, starting from the most relevant
        await foreach ((MemoryRecord memory, double relevance) in matches.WithCancellation(cancellationToken).ConfigureAwait(false))
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
            var size = GPT3Tokenizer.Encode(fact).Count;
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
        await foreach (var x in this.GenerateAnswerAsync(question, facts.ToString())
                           .WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            text.Append(x);
        }

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

        return this._textGenerator.GenerateTextAsync(prompt, new TextGenerationOptions());
    }
}
