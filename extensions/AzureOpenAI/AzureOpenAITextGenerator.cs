// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.AzureOpenAI.Internals;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace Microsoft.KernelMemory.AI.AzureOpenAI;

[Experimental("KMEXP01")]
public sealed class AzureOpenAITextGenerator : ITextGenerator
{
    private readonly AzureOpenAIChatCompletionService _client;
    private readonly ITextTokenizer _textTokenizer;
    private readonly ILogger<AzureOpenAITextGenerator> _log;

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
        this.MaxTokenTotal = config.MaxTokenTotal;

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
