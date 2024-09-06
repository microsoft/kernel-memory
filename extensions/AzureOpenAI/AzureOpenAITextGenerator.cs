// Copyright (c) Microsoft. All rights reserved.

using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Diagnostics;
using OpenAI.Chat;

namespace Microsoft.KernelMemory.AI.AzureOpenAI;

[Experimental("KMEXP01")]
public sealed class AzureOpenAITextGenerator : ITextGenerator
{
    private readonly ITextTokenizer _textTokenizer;
    private readonly AzureOpenAIClient _client;
    private readonly ILogger<AzureOpenAITextGenerator> _log;
    private readonly bool _useTextCompletionProtocol;
    private readonly string _deployment;

    public AzureOpenAITextGenerator(
        AzureOpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null,
        HttpClient? httpClient = null)
    {
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<AzureOpenAITextGenerator>();

        if (textTokenizer == null)
        {
            this._log.LogWarning(
                "Tokenizer not specified, will use {0}. The token count might be incorrect, causing unexpected errors",
                nameof(GPT4oTokenizer));
            textTokenizer = new GPT4oTokenizer();
        }

        this._textTokenizer = textTokenizer;

        if (string.IsNullOrEmpty(config.Endpoint))
        {
            throw new ConfigurationException($"Azure OpenAI: {config.Endpoint} is empty");
        }

        if (string.IsNullOrEmpty(config.Deployment))
        {
            throw new ConfigurationException($"Azure OpenAI: {config.Deployment} is empty");
        }

        this._useTextCompletionProtocol = config.APIType == AzureOpenAIConfig.APITypes.TextCompletion;
        this._deployment = config.Deployment;
        this.MaxTokenTotal = config.MaxTokenTotal;

        switch (config.Auth)
        {
            case AzureOpenAIConfig.AuthTypes.AzureIdentity:
                this._client = new AzureOpenAIClient(new Uri(config.Endpoint), new DefaultAzureCredential());
                break;

            case AzureOpenAIConfig.AuthTypes.ManualTokenCredential:
                this._client = new AzureOpenAIClient(new Uri(config.Endpoint), config.GetTokenCredential());
                break;

            case AzureOpenAIConfig.AuthTypes.APIKey:
                if (string.IsNullOrEmpty(config.APIKey))
                {
                    throw new ConfigurationException($"Azure OpenAI: {config.APIKey} is empty");
                }

                this._client = new AzureOpenAIClient(new Uri(config.Endpoint), new AzureKeyCredential(config.APIKey));
                break;

            default:
                throw new ConfigurationException($"Azure OpenAI: authentication type '{config.Auth:G}' is not supported");
        }
    }

    /// <inheritdoc/>
    public int MaxTokenTotal { get; }

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
        if (this._useTextCompletionProtocol)
        {
            this._log.LogTrace("Sending text generation request, deployment '{0}'", this._deployment);

            var chatCompletionOptions = new ChatCompletionOptions
            {
                MaxTokens = options.MaxTokens,
                Temperature = (float)options.Temperature,
                TopP = (float)options.NucleusSampling,
                FrequencyPenalty = (float)options.FrequencyPenalty,
                PresencePenalty = (float)options.PresencePenalty,
            };

            if (options.StopSequences is { Count: > 0 })
            {
                foreach (var s in options.StopSequences) { chatCompletionOptions.StopSequences.Add(s); }
            }

            if (options.LogitBiases is { Count: > 0 })
            {
                foreach (var (token, bias) in options.LogitBiases) { chatCompletionOptions.LogitBiases.Add(token, (int)bias); }
            }

            ChatMessage chatMessage = ChatMessage.CreateUserMessage(prompt);

            IEnumerable<ChatMessage> messages = new List<ChatMessage> { chatMessage };

            var chatClient = this._client.GetChatClient(this._deployment);
            AsyncCollectionResult<StreamingChatCompletionUpdate> response = chatClient.CompleteChatStreamingAsync(messages: messages, options: chatCompletionOptions, cancellationToken: cancellationToken);

            await foreach (StreamingChatCompletionUpdate update in response.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                foreach (var part in update.ContentUpdate)
                {
                    yield return part.Text;
                }
            }
        }
        else
        {
            this._log.LogTrace("Sending chat message generation request, deployment '{0}'", this._deployment);

            var chatCompletionOptions = new ChatCompletionOptions
            {
                MaxTokens = options.MaxTokens,
                Temperature = (float)options.Temperature,
                TopP = (float)options.NucleusSampling,
                FrequencyPenalty = (float)options.FrequencyPenalty,
                PresencePenalty = (float)options.PresencePenalty,
            };

            if (options.StopSequences is { Count: > 0 })
            {
                foreach (var s in options.StopSequences) { chatCompletionOptions.StopSequences.Add(s); }
            }

            if (options.LogitBiases is { Count: > 0 })
            {
                foreach (var (token, bias) in options.LogitBiases) { chatCompletionOptions.LogitBiases.Add(token, (int)bias); }
            }

            ChatMessage chatMessage = ChatMessage.CreateUserMessage(prompt);

            IEnumerable<ChatMessage> messages = new List<ChatMessage> { chatMessage };

            var chatClient = this._client.GetChatClient(this._deployment);
            AsyncCollectionResult<StreamingChatCompletionUpdate> response = chatClient.CompleteChatStreamingAsync(messages: messages, options: chatCompletionOptions, cancellationToken: cancellationToken);

            await foreach (StreamingChatCompletionUpdate update in response.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                foreach (var part in update.ContentUpdate)
                {
                    yield return part.Text;
                }
            }
        }
    }
}
