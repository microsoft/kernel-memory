// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.OpenAI.Internals;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI;
using OpenAI.Chat;

namespace Microsoft.KernelMemory.AI.OpenAI;

/// <summary>
/// Text generator, supporting OpenAI text and chat completion. The class can be used with any service
/// supporting OpenAI HTTP schema, such as LM Studio HTTP API.
///
/// Note: does not support model name override via request context
///       see https://github.com/microsoft/semantic-kernel/issues/9337
/// </summary>
[Experimental("KMEXP01")]
public sealed class OpenAITextGenerator : ITextGenerator
{
    private readonly OpenAIChatCompletionService _client;
    private readonly ITextTokenizer _textTokenizer;
    private readonly ILogger<OpenAITextGenerator> _log;

    private readonly string _textModel;

    /// <inheritdoc/>
    public int MaxTokenTotal { get; }

    /// <summary>
    /// Create a new instance.
    /// </summary>
    /// <param name="config">Client and model configuration</param>
    /// <param name="textTokenizer">Text tokenizer, possibly matching the model used</param>
    /// <param name="loggerFactory">App logger factory</param>
    /// <param name="httpClient">Optional HTTP client with custom settings</param>
    public OpenAITextGenerator(
        OpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null,
        HttpClient? httpClient = null)
        : this(
            config,
            OpenAIClientBuilder.Build(config, httpClient, loggerFactory),
            textTokenizer,
            loggerFactory)
    {
    }

    /// <summary>
    /// Create a new instance, using the given OpenAI pre-configured client
    /// </summary>
    /// <param name="config">Model configuration</param>
    /// <param name="openAIClient">Custom OpenAI client, already configured</param>
    /// <param name="textTokenizer">Text tokenizer, possibly matching the model used</param>
    /// <param name="loggerFactory">App logger factory</param>
    public OpenAITextGenerator(
        OpenAIConfig config,
        OpenAIClient openAIClient,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
        : this(
            config,
            SkClientBuilder.BuildChatClient(config.TextModel, openAIClient, loggerFactory),
            textTokenizer,
            loggerFactory)
    {
    }

    /// <summary>
    /// Create a new instance, using the given OpenAI pre-configured client
    /// </summary>
    /// <param name="config">Model configuration</param>
    /// <param name="skClient">Custom Semantic Kernel client, already configured</param>
    /// <param name="textTokenizer">Text tokenizer, possibly matching the model used</param>
    /// <param name="loggerFactory">App logger factory</param>
    public OpenAITextGenerator(
        OpenAIConfig config,
        OpenAIChatCompletionService skClient,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
    {
        this._client = skClient;
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<OpenAITextGenerator>();
        this._textModel = config.TextModel;
        this.MaxTokenTotal = config.TextModelMaxTokenTotal;

        if (textTokenizer == null && !string.IsNullOrEmpty(config.TextModelTokenizer))
        {
            textTokenizer = TokenizerFactory.GetTokenizerForEncoding(config.TextModelTokenizer);
        }

        textTokenizer ??= TokenizerFactory.GetTokenizerForModel(config.TextModel);
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
        var skOptions = new OpenAIPromptExecutionSettings
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
            throw new OpenAIException(e.Message, e, isTransient: e.StatusCode.IsTransientError());
        }

        await foreach (StreamingTextContent x in result.WithCancellation(cancellationToken))
        {
            TokenUsage? tokenUsage = null;

            // The last message in the chunk has the usage metadata.
            // https://platform.openai.com/docs/api-reference/chat/create#chat-create-stream_options
            if (x.Metadata?["Usage"] is ChatTokenUsage { } usage)
            {
                this._log.LogTrace("Usage report: input tokens {0}, output tokens {1}, output reasoning tokens {2}",
                    usage.InputTokenCount, usage.OutputTokenCount, usage.OutputTokenDetails?.ReasoningTokenCount ?? 0);

                tokenUsage = new TokenUsage
                {
                    Timestamp = (DateTimeOffset?)x.Metadata["CreatedAt"] ?? DateTimeOffset.UtcNow,
                    ServiceType = "OpenAI",
                    ModelType = Constants.ModelType.TextGeneration,
                    ModelName = this._textModel,
                    ServiceTokensIn = usage!.InputTokenCount,
                    ServiceTokensOut = usage.OutputTokenCount,
                    ServiceReasoningTokens = usage.OutputTokenDetails?.ReasoningTokenCount
                };
            }

            // NOTE: as stated at https://platform.openai.com/docs/api-reference/chat/streaming#chat/streaming-choices,
            // The Choice can also be empty for the last chunk if we set stream_options: { "include_usage": true} to get token counts, so it is possible that
            // x.Text is null, but tokenUsage is not (token usage statistics for the entire request are included in the last chunk).
            if (x.Text is null && tokenUsage is null) { continue; }

            yield return new(x.Text ?? string.Empty, tokenUsage);
        }
    }
}
