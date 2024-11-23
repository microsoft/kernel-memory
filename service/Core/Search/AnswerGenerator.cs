// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Context;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Prompts;

namespace Microsoft.KernelMemory.Search;

[Experimental("KMEXP05")]
internal class AnswerGenerator
{
    private readonly ILogger<AnswerGenerator> _log;
    private readonly IContentModeration? _contentModeration;
    private readonly SearchClientConfig _config;
    private readonly string _answerPrompt;
    private readonly ITextGenerator _textGenerator;

    public AnswerGenerator(
        ITextGenerator textGenerator,
        SearchClientConfig? config = null,
        IPromptProvider? promptProvider = null,
        IContentModeration? contentModeration = null,
        ILoggerFactory? loggerFactory = null)
    {
        this._textGenerator = textGenerator;
        this._contentModeration = contentModeration;
        this._config = config ?? new SearchClientConfig();
        this._config.Validate();
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<AnswerGenerator>();

        promptProvider ??= new EmbeddedPromptProvider();
        this._answerPrompt = promptProvider.ReadPrompt(Constants.PromptNamesAnswerWithFacts);

        if (this._textGenerator == null)
        {
            throw new KernelMemoryException("Text generator not configured");
        }
    }

    internal async Task<MemoryAnswer> GenerateAnswerAsync(
        string question, SearchClientResult result, IContext? context, CancellationToken cancellationToken)
    {
        if (result.FactsAvailableCount > 0 && result.FactsUsedCount == 0)
        {
            this._log.LogError("Unable to inject memories in the prompt, not enough tokens available");
            result.AskResult.NoResultReason = "Unable to use memories";
            return result.AskResult;
        }

        if (result.FactsUsedCount == 0)
        {
            this._log.LogWarning("No memories available");
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
