// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticMemory.Client;
using Microsoft.SemanticMemory.Client.Models;
using Microsoft.SemanticMemory.Core.Diagnostics;
using Microsoft.SemanticMemory.Core.WebService;

namespace Microsoft.SemanticMemory.Core.Search;

public class SearchClient
{
    private const float MinSimilarity = 0.5f;
    private const int MatchesCount = 100;
    private const int AnswerTokens = 300;

    private readonly IKernel _kernel;
    private readonly MemoryClient memoryClient;
    private readonly ILogger<SearchClient> _log;
    private readonly string _prompt;
    private readonly ISKFunction _skFunction;

    public SearchClient(
        MemoryClient memoryClient,
        IKernel kernel,
        ILogger<SearchClient>? log = null)
    {
        this.memoryClient = memoryClient ?? throw new SemanticMemoryException("MemoryClient not configured"); ;
        this._kernel = kernel ?? throw new SemanticMemoryException("Semantic Kernel not configured");
        this._log = log ?? DefaultLogger<SearchClient>.Instance;

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

    public Task<MemoryAnswer> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        return this.SearchAsync(request.UserId, request.Query, cancellationToken);
    }

    public async Task<MemoryAnswer> SearchAsync(string userId, string query, CancellationToken cancellationToken = default)
    {
        var factBuilder = new StringBuilder();
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

        var citationTracker = new HashSet<string>();

        await foreach ((var citation, var partition) in this.memoryClient.QueryMemoryAsync(query, userId, MinSimilarity, MatchesCount, cancellationToken).ConfigureAwait(false))
        {
            factsAvailableCount++;

            // TODO: URL to access the file
            var fact = $"==== [File:{citation.SourceName};Relevance:{partition.Relevance:P1}]:\n{partition.Text}";

            var size = GPT3Tokenizer.Encode(fact).Count;

            // Use the partition/chunk only if there's room for it
            if (size < tokensAvailable)
            {
                factsUsedCount++;
                this._log.LogTrace("Adding text {0} with relevance {1}", factsUsedCount, partition.Relevance);

                factBuilder.AppendLine(fact);
                tokensAvailable -= size;
            }

            // Add each citation as answer source, once.
            if (citationTracker.Add(citation.Link))
            {
                answer.RelevantSources.Add(citation);
            }
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

        answer.Result = await this.GenerateAnswerAsync(query, factBuilder.ToString()).ConfigureAwait(false);

        return answer;
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
