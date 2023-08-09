// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;
using Microsoft.SemanticMemory.Client;
using Microsoft.SemanticMemory.Client.Models;
using Microsoft.SemanticMemory.Core.AI;
using Microsoft.SemanticMemory.Core.Diagnostics;
using Microsoft.SemanticMemory.Core.WebService;

namespace Microsoft.SemanticMemory.Core.Search;

public class SearchClient
{
    private const float MinSimilarity = 0.5f;
    private const int MatchesCount = 100;
    private const int AnswerTokens = 300;

    private readonly MemoryClient memoryClient;
    private readonly ITextGeneration _textGenerator;
    private readonly ILogger<SearchClient> _log;
    private readonly string _prompt;

    public SearchClient(
        MemoryClient memoryClient,
        ITextGeneration textGenerator,
        ILogger<SearchClient>? log = null)
    {
        this.memoryClient = memoryClient ?? throw new SemanticMemoryException("MemoryClient not configured"); ;
        this._textGenerator = textGenerator ?? throw new SemanticMemoryException("Text generator not configured"); ;
        this._log = log ?? DefaultLogger<SearchClient>.Instance;

        this._prompt = "Facts:\n" +
                       "{{$facts}}" +
                       "======\n" +
                       "Given only the facts above, provide a comprehensive/detailed answer.\n" +
                       "You don't know where the knowledge comes from, just answer.\n" +
                       "If you don't have sufficient information, reply with 'INFO NOT FOUND'.\n" +
                       "Question: {{$question}}\n" +
                       "Answer: ";
    }

    public async Task<IList<(MemoryAnswer.Citation, MemoryAnswer.Citation.Partition)>> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        var memories = await this.memoryClient.QueryMemoryAsync(request.UserId, request.Query, MinSimilarity, MatchesCount, cancellationToken).ToArrayAsync(cancellationToken).ConfigureAwait(false);

        return memories ?? Array.Empty<(MemoryAnswer.Citation, MemoryAnswer.Citation.Partition)>();
    }

    public Task<MemoryAnswer> AnswerAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        return this.AnswerAsync(request.UserId, request.Query, cancellationToken);
    }

    public async Task<MemoryAnswer> AnswerAsync(string userId, string query, CancellationToken cancellationToken = default)
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

                facts.AppendLine(fact);
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

        var text = new StringBuilder();
        await foreach (var x in this.GenerateAnswerAsync(query, facts.ToString()).ConfigureAwait(false))
        {
            text.Append(x);
        }

        answer.Result = text.ToString();

        return answer;
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
