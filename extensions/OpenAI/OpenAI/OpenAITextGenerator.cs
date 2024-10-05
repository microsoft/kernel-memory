// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.OpenAI.Internals;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI;

namespace Microsoft.KernelMemory.AI.OpenAI;

/// <summary>
/// Text generator, supporting OpenAI text and chat completion. The class can be used with any service
/// supporting OpenAI HTTP schema, such as LM Studio HTTP API.
/// </summary>
[Experimental("KMEXP01")]
public sealed class OpenAITextGenerator : ITextGenerator
{
    private readonly OpenAIChatCompletionService _client;
    private readonly ITextTokenizer _textTokenizer;
    private readonly ILogger<OpenAITextGenerator> _log;

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
        this.MaxTokenTotal = config.TextModelMaxTokenTotal;

        if (textTokenizer == null)
        {
            this._log.LogWarning(
                "Tokenizer not specified, will use {0}. The token count might be incorrect, causing unexpected errors",
                nameof(GPT4oTokenizer));
            textTokenizer = new GPT4oTokenizer();
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
    public async IAsyncEnumerable<string> GenerateTextAsync(
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
            skOptions.StopSequences = new List<string>();
            foreach (var s in options.StopSequences) { skOptions.StopSequences.Add(s); }
        }

        if (options.TokenSelectionBiases is { Count: > 0 })
        {
            skOptions.TokenSelectionBiases = new Dictionary<int, int>();
            foreach (var (token, bias) in options.TokenSelectionBiases) { skOptions.TokenSelectionBiases.Add(token, (int)bias); }
        }

        this._log.LogTrace("Sending chat message generation request");
        IAsyncEnumerable<StreamingTextContent> result = this._client.GetStreamingTextContentsAsync(prompt, skOptions, cancellationToken: cancellationToken);
        await foreach (StreamingTextContent x in result)
        {
            if (x.Text == null) { continue; }

            yield return x.Text;
        }
    }
}
