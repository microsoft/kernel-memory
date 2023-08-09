// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.Diagnostics;

namespace Microsoft.SemanticMemory.Core.AI.OpenAI;

public class OpenAITextGeneration : ITextGeneration
{
    private readonly ILogger<OpenAITextGeneration> _log;
    private readonly OpenAIClient _client;
    private readonly bool _isTextModel;
    private readonly string _model;

    public OpenAITextGeneration(OpenAIConfig config, ILogger<OpenAITextGeneration>? log)
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

        this._log = log ?? DefaultLogger<OpenAITextGeneration>.Instance;

        if (string.IsNullOrEmpty(config.TextModel))
        {
            throw new ConfigurationException("The OpenAI model name is empty");
        }

        this._isTextModel = (textModels.Contains(config.TextModel.ToLowerInvariant()));
        this._model = config.TextModel;

        OpenAIClientOptions options = new()
        {
            RetryPolicy = new RetryPolicy(maxRetries: Math.Min(0, config.MaxRetries), new SequentialDelayStrategy()),
            Diagnostics =
            {
                IsTelemetryEnabled = Telemetry.IsTelemetryEnabled,
                ApplicationId = Telemetry.HttpUserAgent,
            }
        };

        this._client = new OpenAIClient(config.APIKey, options);
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
                MaxTokens = options.MaxTokens,
                Temperature = (float)options.Temperature,
                NucleusSamplingFactor = (float)options.TopP,
                FrequencyPenalty = (float)options.FrequencyPenalty,
                PresencePenalty = (float)options.PresencePenalty,
                ChoicesPerPrompt = 1,
            };

            if (options.StopSequences is { Count: > 0 })
            {
                foreach (var s in openaiOptions.StopSequences) { options.StopSequences.Add(s); }
            }

            Response<StreamingCompletions>? response = await this._client.GetCompletionsStreamingAsync(this._model, openaiOptions, cancellationToken).ConfigureAwait(false);
            await foreach (StreamingChoice? choice in response.Value.GetChoicesStreaming(cancellationToken))
            {
                await foreach (string? x in choice.GetTextStreaming(cancellationToken))
                {
                    yield return x;
                }
            }
        }
        else
        {
            var openaiOptions = new ChatCompletionsOptions
            {
                MaxTokens = options.MaxTokens,
                Temperature = (float)options.Temperature,
                NucleusSamplingFactor = (float)options.TopP,
                FrequencyPenalty = (float)options.FrequencyPenalty,
                PresencePenalty = (float)options.PresencePenalty,
                ChoiceCount = 1,
            };

            if (options.StopSequences is { Count: > 0 })
            {
                foreach (var s in openaiOptions.StopSequences) { options.StopSequences.Add(s); }
            }

            openaiOptions.Messages.Add(new ChatMessage(ChatRole.System, prompt));

            Response<StreamingChatCompletions>? response = await this._client.GetChatCompletionsStreamingAsync(this._model, openaiOptions, cancellationToken).ConfigureAwait(false);

            await foreach (StreamingChatChoice? choice in response.Value.GetChoicesStreaming(cancellationToken))
            {
                await foreach (ChatMessage? x in choice.GetMessageStreaming(cancellationToken))
                {
                    yield return x.Content;
                }
            }
        }
    }
}
