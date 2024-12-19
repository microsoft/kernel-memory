// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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

        if (this._contentModeration == null || !this._config.UseContentModeration)
        {
            this._log.LogInformation("Content moderation is not enabled.");
        }
    }

    internal async IAsyncEnumerable<MemoryAnswer> GenerateAnswerAsync(
        string question, SearchClientResult result,
        IContext? context, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (result.FactsAvailableCount > 0 && result.FactsUsedCount == 0)
        {
            this._log.LogError("Unable to inject memories in the prompt, not enough tokens available");
            yield return result.InsufficientTokensResult;
            yield break;
        }

        if (result.FactsUsedCount == 0)
        {
            this._log.LogWarning("No memories available");
            yield return result.NoFactsResult;
            yield break;
        }

        var prompt = this.CreatePrompt(question, result.Facts.ToString(), context);

        var tokenUsage = new TokenUsage
        {
            Timestamp = DateTimeOffset.UtcNow,
            ModelType = Constants.ModelType.TextGeneration,
            TokenizerTokensIn = this._textGenerator.CountTokens(prompt)
        };

        result.AskResult.TokenUsage.Add(tokenUsage);

        var completeAnswer = new StringBuilder();

        await foreach (var answerToken in this.GenerateAnswerTokensAsync(prompt, context, cancellationToken).ConfigureAwait(false))
        {
            completeAnswer.Append(answerToken.Text);
            result.AskResult.Result = answerToken.Text;

            tokenUsage.Timestamp = answerToken.TokenUsage?.Timestamp ?? tokenUsage.Timestamp;
            tokenUsage.ServiceType ??= answerToken.TokenUsage?.ServiceType;
            tokenUsage.ModelName ??= answerToken.TokenUsage?.ModelName;
            tokenUsage.ServiceTokensIn ??= answerToken.TokenUsage?.ServiceTokensIn;
            tokenUsage.ServiceTokensOut ??= answerToken.TokenUsage?.ServiceTokensOut;
            tokenUsage.ServiceReasoningTokens ??= answerToken.TokenUsage?.ServiceReasoningTokens;

            yield return result.AskResult;
        }

        // Finalize the answer, checking if it's empty
        result.AskResult.Result = completeAnswer.ToString();
        tokenUsage.TokenizerTokensOut = this._textGenerator.CountTokens(result.AskResult.Result);

        if (string.IsNullOrWhiteSpace(result.AskResult.Result)
            || ValueIsEquivalentTo(result.AskResult.Result, this._config.EmptyAnswer))
        {
            this._log.LogInformation("No relevant memories found, returning empty answer.");
            yield return result.NoFactsResult;
            yield break;
        }

        this._log.LogSensitive("Answer: {0}", result.AskResult.Result);

        if (this._config.UseContentModeration
            && this._contentModeration != null
            && !await this._contentModeration.IsSafeAsync(result.AskResult.Result, cancellationToken).ConfigureAwait(false))
        {
            this._log.LogWarning("Unsafe answer detected. Returning error message instead.");
            yield return result.UnsafeAnswerResult;
        }
    }

    private string CreatePrompt(string question, string facts, IContext? context)
    {
        question = question.Trim();
        question = question.EndsWith('?') ? question : $"{question}?";

        string emptyAnswer = context.GetCustomEmptyAnswerTextOrDefault(this._config.EmptyAnswer);

        string prompt = context.GetCustomRagPromptOrDefault(this._answerPrompt);
        prompt = prompt.Replace("{{$facts}}", facts.Trim(), StringComparison.OrdinalIgnoreCase);
        prompt = prompt.Replace("{{$input}}", question, StringComparison.OrdinalIgnoreCase);
        prompt = prompt.Replace("{{$notFound}}", emptyAnswer, StringComparison.OrdinalIgnoreCase);

        this._log.LogInformation("New prompt: {0}", prompt);

        return prompt;
    }

    private IAsyncEnumerable<GeneratedTextContent> GenerateAnswerTokensAsync(string prompt, IContext? context, CancellationToken cancellationToken)
    {
        int maxTokens = context.GetCustomRagMaxTokensOrDefault(this._config.AnswerTokens);
        double temperature = context.GetCustomRagTemperatureOrDefault(this._config.Temperature);
        double nucleusSampling = context.GetCustomRagNucleusSamplingOrDefault(this._config.TopP);

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
