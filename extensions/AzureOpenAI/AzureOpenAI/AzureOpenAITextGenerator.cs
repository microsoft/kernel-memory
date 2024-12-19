// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.AzureOpenAI.Internals;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using OpenAI.Chat;

namespace Microsoft.KernelMemory.AI.AzureOpenAI;

/// <summary>
/// Azure OpenAI connector
///
/// Note: does not support model name override via request context
///       see https://github.com/microsoft/semantic-kernel/issues/9337
/// </summary>
[Experimental("KMEXP01")]
public sealed class AzureOpenAITextGenerator : ITextGenerator
{
    private readonly AzureOpenAIChatCompletionService _client;
    private readonly ITextTokenizer _textTokenizer;
    private readonly ILogger<AzureOpenAITextGenerator> _log;

    private readonly string _deployment;

    /// <inheritdoc/>
    public int MaxTokenTotal { get; }

    /// <summary>
    /// Create a new instance.
    /// </summary>
    /// <param name="config">Client and service configuration</param>
    /// <param name="textTokenizer">Text tokenizer, possibly matching the model used</param>
    /// <param name="loggerFactory">App logger factory</param>
    /// <param name="httpClient">Optional HTTP client with custom settings</param>
    public AzureOpenAITextGenerator(
        AzureOpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null,
        HttpClient? httpClient = null)
        : this(
            config,
            AzureOpenAIClientBuilder.Build(config, httpClient, loggerFactory),
            textTokenizer,
            loggerFactory)
    {
    }

    /// <summary>
    /// Create a new instance.
    /// </summary>
    /// <param name="config">Client and service configuration</param>
    /// <param name="azureClient">Azure OpenAI client with custom settings</param>
    /// <param name="textTokenizer">Text tokenizer, possibly matching the model used</param>
    /// <param name="loggerFactory">App logger factory</param>
    public AzureOpenAITextGenerator(
        AzureOpenAIConfig config,
        AzureOpenAIClient azureClient,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
        : this(
            config,
            SkClientBuilder.BuildChatClient(config.Deployment, azureClient, loggerFactory),
            textTokenizer,
            loggerFactory)
    {
    }

    /// <summary>
    /// Create a new instance.
    /// </summary>
    /// <param name="config"></param>
    /// <param name="skClient"></param>
    /// <param name="textTokenizer"></param>
    /// <param name="loggerFactory"></param>
    /// <exception cref="ConfigurationException"></exception>
    public AzureOpenAITextGenerator(
        AzureOpenAIConfig config,
        AzureOpenAIChatCompletionService skClient,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
    {
        this._client = skClient;
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<AzureOpenAITextGenerator>();
        this._deployment = config.Deployment;
        this.MaxTokenTotal = config.MaxTokenTotal;

        textTokenizer ??= TokenizerFactory.GetTokenizerForEncoding(config.Tokenizer);
        if (textTokenizer == null)
        {
            textTokenizer = new O200KTokenizer();
            this._log.LogWarning(
                "Tokenizer not specified, will use {0}. The token count might be incorrect, causing unexpected errors",
                textTokenizer.GetType().FullName);
        }

        this._textTokenizer = textTokenizer;
    }

    /// <inheritdoc/>
    public int CountTokens(string text)
    {
        return this._textTokenizer.CountTokens(text);
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetTokens(string text)
    {
        return this._textTokenizer.GetTokens(text);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<GeneratedTextContent> GenerateTextAsync(
        string prompt,
        TextGenerationOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var skOptions = new AzureOpenAIPromptExecutionSettings
        {
            MaxTokens = options.MaxTokens,
            Temperature = options.Temperature,
            FrequencyPenalty = options.FrequencyPenalty,
            PresencePenalty = options.PresencePenalty,
            TopP = options.NucleusSampling
        };

        if (options.StopSequences is { Count: > 0 })
        {
            skOptions.StopSequences = [];
            foreach (var s in options.StopSequences) { skOptions.StopSequences.Add(s); }
        }

        if (options.TokenSelectionBiases is { Count: > 0 })
        {
            skOptions.TokenSelectionBiases = new Dictionary<int, int>();
            foreach (var (token, bias) in options.TokenSelectionBiases) { skOptions.TokenSelectionBiases.Add(token, (int)bias); }
        }

        this._log.LogTrace("Sending chat message generation request");
        IAsyncEnumerable<StreamingTextContent> result;
        try
        {
            result = this._client.GetStreamingTextContentsAsync(prompt, skOptions, cancellationToken: cancellationToken);
        }
        catch (HttpOperationException e)
        {
            throw new AzureOpenAIException(e.Message, e, isTransient: e.StatusCode.IsTransientError());
        }

        await foreach (StreamingTextContent x in result.WithCancellation(cancellationToken))
        {
            TokenUsage? tokenUsage = null;

            // The last message includes tokens usage metadata.
            // https://platform.openai.com/docs/api-reference/chat/create#chat-create-stream_options
            if (x.Metadata?["Usage"] is ChatTokenUsage usage)
            {
                this._log.LogTrace("Usage report: input tokens: {InputTokenCount}, output tokens: {OutputTokenCount}, output reasoning tokens: {ReasoningTokenCount}",
                    usage.InputTokenCount, usage.OutputTokenCount, usage.OutputTokenDetails?.ReasoningTokenCount ?? 0);

                tokenUsage = new TokenUsage
                {
                    Timestamp = (DateTimeOffset?)x.Metadata["CreatedAt"] ?? DateTimeOffset.UtcNow,
                    ServiceType = "Azure OpenAI",
                    ModelType = Constants.ModelType.TextGeneration,
                    ModelName = this._deployment,
                    ServiceTokensIn = usage.InputTokenCount,
                    ServiceTokensOut = usage.OutputTokenCount,
                    ServiceReasoningTokens = usage.OutputTokenDetails?.ReasoningTokenCount
                };
            }

            // NOTE: as stated at https://platform.openai.com/docs/api-reference/chat/streaming#chat/streaming-choices,
            // the Choice can also be empty for the last chunk if we set stream_options: { "include_usage": true} to get token counts, so it is possible that
            // x.Text is null, but tokenUsage is not (token usage statistics for the entire request are included in the last chunk).
            if (x.Text is null && tokenUsage is null) { continue; }

            yield return new(x.Text ?? string.Empty, tokenUsage);
        }
    }
}
