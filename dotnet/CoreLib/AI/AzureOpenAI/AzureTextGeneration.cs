// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.Diagnostics;

namespace Microsoft.SemanticMemory.Core.AI.AzureOpenAI;

public class AzureTextGeneration : ITextGeneration
{
    private readonly ILogger<AzureTextGeneration> _log;
    private readonly OpenAIClient _client;
    private readonly bool _isTextModel;
    private readonly string _deployment;

    public AzureTextGeneration(AzureOpenAIConfig config, ILogger<AzureTextGeneration>? log)
    {
        this._log = log ?? DefaultLogger<AzureTextGeneration>.Instance;

        if (string.IsNullOrEmpty(config.Endpoint))
        {
            throw new ConfigurationException("The Azure OpenAI endpoint is empty");
        }

        if (string.IsNullOrEmpty(config.Deployment))
        {
            throw new ConfigurationException("The Azure OpenAI deployment name is empty");
        }

        this._isTextModel = config.APIType == AzureOpenAIConfig.APITypes.TextCompletion;
        this._deployment = config.Deployment;

        OpenAIClientOptions options = new()
        {
            RetryPolicy = new RetryPolicy(maxRetries: Math.Min(0, config.MaxRetries), new SequentialDelayStrategy()),
            Diagnostics =
            {
                IsTelemetryEnabled = Telemetry.IsTelemetryEnabled,
                ApplicationId = Telemetry.HttpUserAgent,
            }
        };

        switch (config.Auth)
        {
            case AzureOpenAIConfig.AuthTypes.AzureIdentity:
                this._client = new OpenAIClient(new Uri(config.Endpoint), new DefaultAzureCredential(), options);
                break;

            case AzureOpenAIConfig.AuthTypes.APIKey:
                if (string.IsNullOrEmpty(config.APIKey))
                {
                    throw new ConfigurationException("The Azure OpenAI API key is empty");
                }

                this._client = new OpenAIClient(new Uri(config.Endpoint), new AzureKeyCredential(config.APIKey), options);
                break;

            case AzureOpenAIConfig.AuthTypes.ManualTokenCredential:
                this._client = new OpenAIClient(new Uri(config.Endpoint), config.GetTokenCredential(), options);
                break;

            default:
                throw new ConfigurationException($"Azure OpenAI authentication type not supported: {config.Auth:G}");
        }
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

            Response<StreamingCompletions>? response = await this._client.GetCompletionsStreamingAsync(this._deployment, openaiOptions, cancellationToken).ConfigureAwait(false);
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

            Response<StreamingChatCompletions>? response = await this._client.GetChatCompletionsStreamingAsync(this._deployment, openaiOptions, cancellationToken).ConfigureAwait(false);

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
