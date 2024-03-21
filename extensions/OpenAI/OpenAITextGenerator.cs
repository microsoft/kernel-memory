// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.Diagnostics;

namespace Microsoft.KernelMemory.AI.OpenAI;

/// <summary>
/// Text generator, supporting OpenAI text and chat completion. The class can be used with any service
/// supporting OpenAI HTTP schema, such as LM Studio HTTP API.
/// </summary>
public class OpenAITextGenerator : ITextGenerator
{
    private readonly OpenAIClient _client;
    private readonly ILogger<OpenAITextGenerator> _log;
    private ITextTokenizer? _textTokenizer;
    private bool _useTextCompletionProtocol;
    private string? _model;

    /// <summary>
    /// Legacy OpenAI models using the old text generation protocol.
    /// </summary>
    private static readonly List<string> s_textModels = new()
    {
        "text-ada-001",
        "text-babbage-001",
        "text-curie-001",
        "text-davinci-001",
        "text-davinci-002",
        "text-davinci-003",
        "gpt-3.5-turbo-instruct"
    };

    /// <inheritdoc/>
    public int MaxTokenTotal { get; }

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
    {
        this._log = loggerFactory?.CreateLogger<OpenAITextGenerator>() ?? DefaultLogger<OpenAITextGenerator>.Instance;

        this.MaxTokenTotal = config.TextModelMaxTokenTotal;
        this.SetCompletionType(config);
        this.SetTokenizer(textTokenizer);
        this._client = openAIClient;
    }

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
    {
        this._log = loggerFactory?.CreateLogger<OpenAITextGenerator>() ?? DefaultLogger<OpenAITextGenerator>.Instance;

        this.MaxTokenTotal = config.TextModelMaxTokenTotal;
        this.SetCompletionType(config);
        this.SetTokenizer(textTokenizer);
        this._client = OpenAIClientBuilder.BuildOpenAIClient(config, httpClient);
    }

    /// <summary>
    /// Create a new instance.
    /// </summary>
    /// <param name="config">Client and model configuration</param>
    /// <param name="textTokenizer">Text tokenizer, possibly matching the model used</param>
    /// <param name="log">Application logger</param>
    /// <param name="httpClient">Optional HTTP client with custom settings</param>
    public OpenAITextGenerator(
        OpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        ILogger<OpenAITextGenerator>? log = null,
        HttpClient? httpClient = null)
    {
        this._log = log ?? DefaultLogger<OpenAITextGenerator>.Instance;

        this.MaxTokenTotal = config.TextModelMaxTokenTotal;
        this.SetCompletionType(config);
        this.SetTokenizer(textTokenizer);
        this._client = OpenAIClientBuilder.BuildOpenAIClient(config, httpClient);
    }

    /// <inheritdoc/>
    public int CountTokens(string text)
    {
        return this._textTokenizer!.CountTokens(text);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> GenerateTextAsync(
        string prompt,
        TextGenerationOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (this._useTextCompletionProtocol)
        {
            var openaiOptions = new CompletionsOptions
            {
                Prompts = { prompt },
                DeploymentName = this._model,
                MaxTokens = options.MaxTokens,
                Temperature = (float)options.Temperature,
                NucleusSamplingFactor = (float)options.TopP,
                FrequencyPenalty = (float)options.FrequencyPenalty,
                PresencePenalty = (float)options.PresencePenalty,
                ChoicesPerPrompt = 1,
            };

            if (options.StopSequences is { Count: > 0 })
            {
                foreach (var s in options.StopSequences) { openaiOptions.StopSequences.Add(s); }
            }

            if (options.TokenSelectionBiases is { Count: > 0 })
            {
                foreach (var (token, bias) in options.TokenSelectionBiases) { openaiOptions.TokenSelectionBiases.Add(token, (int)bias); }
            }

            StreamingResponse<Completions>? response = await this._client.GetCompletionsStreamingAsync(openaiOptions, cancellationToken).ConfigureAwait(false);
            await foreach (Completions? completions in response.EnumerateValues().WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                foreach (Choice? choice in completions.Choices)
                {
                    yield return choice.Text;
                }
            }
        }
        else
        {
            var openaiOptions = new ChatCompletionsOptions
            {
                DeploymentName = this._model,
                MaxTokens = options.MaxTokens,
                Temperature = (float)options.Temperature,
                NucleusSamplingFactor = (float)options.TopP,
                FrequencyPenalty = (float)options.FrequencyPenalty,
                PresencePenalty = (float)options.PresencePenalty,
                // ChoiceCount = 1,
            };

            if (options.StopSequences is { Count: > 0 })
            {
                foreach (var s in options.StopSequences) { openaiOptions.StopSequences.Add(s); }
            }

            if (options.TokenSelectionBiases is { Count: > 0 })
            {
                foreach (var (token, bias) in options.TokenSelectionBiases) { openaiOptions.TokenSelectionBiases.Add(token, (int)bias); }
            }

            openaiOptions.Messages.Add(new ChatRequestSystemMessage(prompt));

            StreamingResponse<StreamingChatCompletionsUpdate>? response = await this._client.GetChatCompletionsStreamingAsync(openaiOptions, cancellationToken).ConfigureAwait(false);
            await foreach (StreamingChatCompletionsUpdate? update in response.EnumerateValues().WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return update.ContentUpdate;
            }
        }
    }

    private void SetCompletionType(OpenAIConfig config)
    {
        if (string.IsNullOrEmpty(config.TextModel))
        {
            throw new ConfigurationException("The OpenAI model name is empty");
        }

        this._model = config.TextModel;

        switch (config.TextGenerationType)
        {
            case OpenAIConfig.TextGenerationTypes.Auto:
                this._useTextCompletionProtocol = (s_textModels.Contains(config.TextModel.ToLowerInvariant()));
                break;
            case OpenAIConfig.TextGenerationTypes.TextCompletion:
                this._useTextCompletionProtocol = true;
                break;
            case OpenAIConfig.TextGenerationTypes.Chat:
                this._useTextCompletionProtocol = false;
                break;
            default:
                throw new ArgumentOutOfRangeException($"Unsupported text completion type '{config.TextGenerationType:G}'");
        }
    }

    private void SetTokenizer(ITextTokenizer? textTokenizer = null)
    {
        if (textTokenizer == null)
        {
            this._log.LogWarning(
                "Tokenizer not specified, will use {0}. The token count might be incorrect, causing unexpected errors",
                nameof(DefaultGPTTokenizer));
            textTokenizer = new DefaultGPTTokenizer();
        }

        this._textTokenizer = textTokenizer;
    }
}
