// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.Tokenizers;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.Diagnostics;

namespace Microsoft.KernelMemory.AI.OpenAI;

public class OpenAITextGenerator : ITextGenerator
{
    private readonly ILogger<OpenAITextGenerator> _log;
    private readonly ITextTokenizer _textTokenizer;
    private readonly OpenAIClient _client;
    private readonly bool _isTextModel;
    private readonly string _model;

    /// <inheritdoc/>
    public int MaxTokenTotal { get; }

    public OpenAITextGenerator(
        OpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
        : this(config, textTokenizer, loggerFactory?.CreateLogger<OpenAITextGenerator>())
    {
    }

    public OpenAITextGenerator(
        OpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        ILogger<OpenAITextGenerator>? log = null)
    {
        var textModels = new List<string>
        {
            "text-ada-001",
            "text-babbage-001",
            "text-curie-001",
            "text-davinci-001",
            "text-davinci-002",
            "text-davinci-003",
        };

        this._log = log ?? DefaultLogger<OpenAITextGenerator>.Instance;

        if (textTokenizer == null)
        {
            this._log.LogWarning(
                "Tokenizer not specified, will use {0}. The token count might be incorrect, causing unexpected errors",
                nameof(DefaultGPTTokenizer));
            textTokenizer = new DefaultGPTTokenizer();
        }

        this._textTokenizer = textTokenizer;

        if (string.IsNullOrEmpty(config.TextModel))
        {
            throw new ConfigurationException("The OpenAI model name is empty");
        }

        this._isTextModel = (textModels.Contains(config.TextModel.ToLowerInvariant()));
        this._model = config.TextModel;
        this.MaxTokenTotal = config.TextModelMaxTokenTotal;

        OpenAIClientOptions options = new()
        {
            RetryPolicy = new RetryPolicy(maxRetries: Math.Max(0, config.MaxRetries), new SequentialDelayStrategy()),
            Diagnostics =
            {
                IsTelemetryEnabled = Telemetry.IsTelemetryEnabled,
                ApplicationId = Telemetry.HttpUserAgent,
            }
        };

        this._client = new OpenAIClient(config.APIKey, options);
    }

    /// <inheritdoc/>
    public int CountTokens(string text)
    {
        return this._textTokenizer.CountTokens(text);
    }

    public async IAsyncEnumerable<string> GenerateTextAsync(
        string prompt,
        TextGenerationOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (this._isTextModel)
        {
            var openaiOptions = new CompletionsOptions
            {
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

            openaiOptions.Messages.Add(new ChatMessage(ChatRole.System, prompt));

            StreamingResponse<StreamingChatCompletionsUpdate>? response = await this._client.GetChatCompletionsStreamingAsync(openaiOptions, cancellationToken).ConfigureAwait(false);
            await foreach (StreamingChatCompletionsUpdate? update in response.EnumerateValues().WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return update.ContentUpdate;
            }
        }
    }
}
