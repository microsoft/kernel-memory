// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticMemory.Client;
using Microsoft.SemanticMemory.Client.Models;
using Microsoft.SemanticMemory.Core.Diagnostics;
using Microsoft.SemanticMemory.Core.MemoryStorage;
using Microsoft.SemanticMemory.Core.WebService;

namespace Microsoft.SemanticMemory.Core.Search;

public class SearchClient
{
    private const float MinSimilarity = 0.5f;
    private const int MatchesCount = 100;
    private const int AnswerTokens = 300;

    private readonly IKernel _kernel;
    private readonly ISemanticMemoryVectorDb _vectorDb;
    private readonly ITextEmbeddingGeneration _embeddingGenerator;
    private readonly ILogger<SearchClient> _log;
    private readonly string _prompt;
    private readonly ISKFunction _skFunction;

    public SearchClient(
        ISemanticMemoryVectorDb vectorDb,
        ITextEmbeddingGeneration embeddingGenerator,
        IKernel kernel,
        ILogger<SearchClient>? log = null)
    {
        this._vectorDb = vectorDb;
        this._embeddingGenerator = embeddingGenerator;
        this._kernel = kernel;
        this._log = log ?? DefaultLogger<SearchClient>.Instance;

        if (this._embeddingGenerator == null) { throw new SemanticMemoryException("Embedding generator not configured"); }

        if (this._vectorDb == null) { throw new SemanticMemoryException("Search vector DB not configured"); }

        if (this._kernel == null) { throw new SemanticMemoryException("Semantic Kernel not configured"); }

        this._prompt = "Facts:\n" +
                       "{{$facts}}" +
                       "======\n" +
                       "Given only the facts above, provide a comprehensive/detailed answer.\n" +
                       "You don't know where the knowledge comes from, just answer.\n" +
                       "If you don't have sufficient information, reply with 'INFO NOT FOUND'.\n" +
                       "Question: {{$question}}\n" +
                       "Answer: ";

        this._skFunction = this._kernel.CreateSemanticFunction(this._prompt.Trim(), maxTokens: AnswerTokens, temperature: 0);
    }

    public Task<MemoryAnswer> SearchAsync(SearchRequest request)
    {
        return this.SearchAsync(request.UserId, request.Query);
    }

    public async Task<MemoryAnswer> SearchAsync(string userId, string query)
    {
        var facts = string.Empty;
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
        IAsyncEnumerable<(MemoryRecord, double)> matches = this._vectorDb.GetNearestMatchesAsync(
            indexName: userId, embedding, MatchesCount, MinSimilarity, false);

        await foreach ((MemoryRecord, double) memory in matches)
        {
            if (!memory.Item1.Tags.ContainsKey(Constants.ReservedPipelineIdTag))
            {
                this._log.LogError("The memory record is missing the '{0}' tag", Constants.ReservedPipelineIdTag);
            }

            if (!memory.Item1.Tags.ContainsKey(Constants.ReservedFileIdTag))
            {
                this._log.LogError("The memory record is missing the '{0}' tag", Constants.ReservedFileIdTag);
            }

            if (!memory.Item1.Tags.ContainsKey(Constants.ReservedFileTypeTag))
            {
                this._log.LogError("The memory record is missing the '{0}' tag", Constants.ReservedFileTypeTag);
            }

            // Note: a document can be composed by multiple files
            string documentId = memory.Item1.Tags[Constants.ReservedPipelineIdTag].FirstOrDefault() ?? string.Empty;

            // Identify the file in case there are multiple files
            string fileId = memory.Item1.Tags[Constants.ReservedFileIdTag].FirstOrDefault() ?? string.Empty;

            // TODO: URL to access the file
            string linkToFile = $"{documentId}/{fileId}";

            string fileContentType = memory.Item1.Tags[Constants.ReservedFileTypeTag].FirstOrDefault() ?? string.Empty;
            string fileName = memory.Item1.Metadata["file_name"].ToString() ?? string.Empty;

            factsAvailableCount++;
            var partitionText = memory.Item1.Metadata["text"].ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(partitionText))
            {
                this._log.LogError("The document partition is empty, user: {0}, doc: {1}", memory.Item1.Owner, memory.Item1.Id);
                continue;
            }

            // TODO: add file age in days, to push relevance of newer documents
            var fact = $"==== [File:{fileName};Relevance:{memory.Item2:P1}]:\n{partitionText}\n";

            // Use the partition/chunk only if there's room for it
            var size = GPT3Tokenizer.Encode(fact).Count;
            if (size < tokensAvailable)
            {
                factsUsedCount++;
                this._log.LogTrace("Adding text {0} with relevance {1}", factsUsedCount, memory.Item2);

                facts += fact;
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
                DateTimeOffset.TryParse(memory.Item1.Metadata["last_update"].ToString(), CultureInfo.InvariantCulture, out var lastUpdate);
                citation.Partitions.Add(new MemoryAnswer.Citation.Partition
                {
                    Text = partitionText,
                    Relevance = (float)memory.Item2,
                    SizeInTokens = size,
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

        answer.Result = await this.GenerateAnswerAsync(query, facts).ConfigureAwait(false);

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

    private async Task<string> GenerateAnswerAsync(string query, string facts)
    {
        SKContext context = this._kernel.CreateNewContext();
        context["facts"] = facts.Trim();

        query = query.Trim();
        context["question"] = query;
        context["question"] = query.EndsWith('?') ? query : $"{query}?";

        SKContext result = await this._skFunction.InvokeAsync(context).ConfigureAwait(false);

        if (result.ErrorOccurred)
        {
            this._log.LogError(result.LastException, "Failed to generate answer: {0}", result.LastErrorDescription);
            throw result.LastException ?? new SemanticMemoryException($"Failed to generate answer: {result.LastErrorDescription}");
        }

        return result.Result.Trim();
    }
}
