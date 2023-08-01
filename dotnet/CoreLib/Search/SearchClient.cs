// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticMemory.Client;
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
    }

    public Task<string> SearchAsync(SearchRequest request)
    {
        return this.SearchAsync(request.UserId, request.Query);
    }

    public async Task<string> SearchAsync(string userId, string query)
    {
        this._log.LogTrace("Generating embedding for the query");
        IList<Embedding<float>> embeddings = await this._embeddingGenerator
            .GenerateEmbeddingsAsync(new List<string> { query }).ConfigureAwait(false);
        Embedding<float> embedding;
        if (embeddings.Count == 0)
        {
            throw new SemanticMemoryException("Failed to generate embedding for the given question");
        }

        embedding = embeddings.First();

        var prompt = "Facts:\n" +
                     "{{$facts}}" +
                     "======\n" +
                     "Given only the facts above, provide a comprehensive/detailed answer.\n" +
                     "You don't know where the knowledge comes from, just answer.\n" +
                     "If you don't have sufficient information, replay with 'INFO NOT FOUND'.\n" +
                     "Question: {{$question}}\n" +
                     "Answer: ";

        this._log.LogTrace("Fetching relevant memories");
        IAsyncEnumerable<(MemoryRecord, double)> matches = this._vectorDb.GetNearestMatchesAsync(
            indexName: userId, embedding, MatchesCount, MinSimilarity, false);

        var facts = string.Empty;
        var tokensAvailable = 8000
                              - GPT3Tokenizer.Encode(prompt).Count
                              - GPT3Tokenizer.Encode(query).Count
                              - AnswerTokens;

        var factsUsedCount = 0;
        var factsAvailableCount = 0;
        await foreach ((MemoryRecord, double) memory in matches)
        {
            factsAvailableCount++;
            var partition = memory.Item1.Metadata["text"].ToString()?.Trim() ?? "";
            var fact = $"======\n{partition}\n";
            var size = GPT3Tokenizer.Encode(fact).Count;
            if (size < tokensAvailable)
            {
                factsUsedCount++;
                facts += fact;
                tokensAvailable -= size;
                continue;
            }

            break;
        }

        if (factsAvailableCount == 0)
        {
            this._log.LogWarning("No memories available");
            return "INFO NOT FOUND";
        }

        if (factsAvailableCount > 0 && factsUsedCount == 0)
        {
            this._log.LogError("Unable to inject memories in the prompt, not enough token available");
            return "INFO NOT FOUND";
        }

        var context = this._kernel.CreateNewContext();
        context["facts"] = facts.Trim();

        query = query.Trim();
        context["question"] = query.Trim();
        context["question"] = query.EndsWith('?') ? query : $"{query}?";

        var skFunction = this._kernel.CreateSemanticFunction(prompt.Trim(), maxTokens: AnswerTokens, temperature: 0);
        SKContext result = await skFunction.InvokeAsync(context).ConfigureAwait(false);

        return result.Result;
    }
}
