// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;
using Microsoft.SemanticMemory.Client;
using Microsoft.SemanticMemory.Client.Models;
using Microsoft.SemanticMemory.Core.AI;
using Microsoft.SemanticMemory.Core.Diagnostics;
using Microsoft.SemanticMemory.Core.MemoryStorage;
using Microsoft.SemanticMemory.Core.WebService;

namespace Microsoft.SemanticMemory.Core.Search;

public class SearchClient
{
    private const float MinSimilarity = 0.5f;
    private const int MatchesCount = 100;
    private const int AnswerTokens = 300;

    private readonly ISemanticMemoryVectorDb _vectorDb;
    private readonly ITextEmbeddingGeneration _embeddingGenerator;
    private readonly ITextGeneration _textGenerator;
    private readonly ILogger<SearchClient> _log;
    private readonly string _prompt;

    public SearchClient(
        ISemanticMemoryVectorDb vectorDb,
        ITextEmbeddingGeneration embeddingGenerator,
        ITextGeneration textGenerator,
        ILogger<SearchClient>? log = null)
    {
        this._vectorDb = vectorDb;
        this._embeddingGenerator = embeddingGenerator;
        this._textGenerator = textGenerator;
        this._log = log ?? DefaultLogger<SearchClient>.Instance;

        if (this._embeddingGenerator == null) { throw new SemanticMemoryException("Embedding generator not configured"); }

        if (this._vectorDb == null) { throw new SemanticMemoryException("Search vector DB not configured"); }

        if (this._textGenerator == null) { throw new SemanticMemoryException("Text generator not configured"); }

        this._prompt = "Facts:\n" +
                       "{{$facts}}" +
                       "======\n" +
                       "Given only the facts above, provide a comprehensive/detailed answer.\n" +
                       "You don't know where the knowledge comes from, just answer.\n" +
                       "If you don't have sufficient information, reply with 'INFO NOT FOUND'.\n" +
                       "Question: {{$question}}\n" +
                       "Answer: ";
    }

    public Task<MemoryAnswer> AskAsync(MemoryQuery query, CancellationToken cancellationToken = default)
    {
        return this.AskAsync(query.UserId, query.Query, query.Filter, cancellationToken);
    }

    public async Task<MemoryAnswer> AskAsync(string userId, string query, MemoryFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var facts = new StringBuilder();
        var tokensAvailable = 8000
                              - GPT3Tokenizer.Encode(this._prompt).Count
                              - GPT3Tokenizer.Encode(query).Count
                              - AnswerTokens;

        var factsUsedCount = 0;
        var factsAvailableCount = 0;

        var answer = new MemoryAnswer
        {
            Query = query,
            Result = "INFO NOT FOUND",
        };

        var embedding = await this.GenerateEmbeddingAsync(query).ConfigureAwait(false);

        this._log.LogTrace("Fetching relevant memories");
        IAsyncEnumerable<(MemoryRecord, double)> matches = this._vectorDb.GetSimilarListAsync(
            indexName: userId, embedding, MatchesCount, MinSimilarity, filter, false, cancellationToken: cancellationToken);

        // Memories are sorted by relevance, starting from the most relevant
        await foreach ((MemoryRecord memory, double relevance) in matches.WithCancellation(cancellationToken))
        {
            if (!memory.Tags.ContainsKey(Constants.ReservedPipelineIdTag))
            {
                this._log.LogError("The memory record is missing the '{0}' tag", Constants.ReservedPipelineIdTag);
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
            string documentId = memory.Tags[Constants.ReservedPipelineIdTag].FirstOrDefault() ?? string.Empty;

            // Identify the file in case there are multiple files
            string fileId = memory.Tags[Constants.ReservedFileIdTag].FirstOrDefault() ?? string.Empty;

            // TODO: URL to access the file
            string linkToFile = $"{documentId}/{fileId}";

            string fileContentType = memory.Tags[Constants.ReservedFileTypeTag].FirstOrDefault() ?? string.Empty;
            string fileName = memory.Metadata["file_name"].ToString() ?? string.Empty;

            factsAvailableCount++;
            var partitionText = memory.Metadata["text"].ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(partitionText))
            {
                this._log.LogError("The document partition is empty, user: {0}, doc: {1}", memory.Owner, memory.Id);
                continue;
            }

            // TODO: add file age in days, to push relevance of newer documents
            var fact = $"==== [File:{fileName};Relevance:{relevance:P1}]:\n{partitionText}\n";

            // Use the partition/chunk only if there's room for it
            var size = GPT3Tokenizer.Encode(fact).Count;
            if (size < tokensAvailable)
            {
                factsUsedCount++;
                this._log.LogTrace("Adding text {0} with relevance {1}", factsUsedCount, relevance);

                facts.Append(fact);
                tokensAvailable -= size;

                // If the file is already in the list of citations, only add the partition
                var citation = answer.RelevantSources.FirstOrDefault(x => x.Link == linkToFile);
                if (citation == null)
                {
                    citation = new MemoryAnswer.Citation();
                    answer.RelevantSources.Add(citation);
                }

                // Add the partition to the list of citations
                citation.Link = linkToFile;
                citation.SourceContentType = fileContentType;
                citation.SourceName = fileName;

#pragma warning disable CA1806 // it's ok if parsing fails
                DateTimeOffset.TryParse(memory.Metadata["last_update"].ToString(), out var lastUpdate);
#pragma warning restore CA1806

                citation.Partitions.Add(new MemoryAnswer.Citation.Partition
                {
                    Text = partitionText,
                    Relevance = (float)relevance,
                    LastUpdate = lastUpdate,
                });

                continue;
            }

            break;
        }

        if (factsAvailableCount > 0 && factsUsedCount == 0)
        {
            this._log.LogError("Unable to inject memories in the prompt, not enough tokens available");
            return new MemoryAnswer { Query = query, Result = "INFO NOT FOUND" };
        }

        if (factsUsedCount == 0)
        {
            this._log.LogWarning("No memories available");
            return new MemoryAnswer { Query = query, Result = "INFO NOT FOUND" };
        }

        var text = new StringBuilder();
        await foreach (var x in this.GenerateAnswerAsync(query, facts.ToString()).ConfigureAwait(false))
        {
            text.Append(x);
        }

        answer.Result = text.ToString();

        return answer;
    }

    private async Task<Embedding<float>> GenerateEmbeddingAsync(string text)
    {
        this._log.LogTrace("Generating embedding for the query");
        IList<Embedding<float>> embeddings = await this._embeddingGenerator
            .GenerateEmbeddingsAsync(new List<string> { text }).ConfigureAwait(false);
        if (embeddings.Count == 0)
        {
            throw new SemanticMemoryException("Failed to generate embedding for the given question");
        }

        return embeddings.First();
    }

    private IAsyncEnumerable<string> GenerateAnswerAsync(string question, string facts)
    {
        var prompt = this._prompt;
        prompt = prompt.Replace("{{$facts}}", facts.Trim(), StringComparison.OrdinalIgnoreCase);

        question = question.Trim();
        question = question.EndsWith('?') ? question : $"{question}?";
        prompt = prompt.Replace("{{$question}}", question, StringComparison.OrdinalIgnoreCase);

        return this._textGenerator.GenerateTextAsync(prompt, new TextGenerationOptions());
    }
}
