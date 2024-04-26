// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.Anthropic.Client;
using Microsoft.KernelMemory.Diagnostics;

namespace Microsoft.KernelMemory.AI.Anthropic;

internal sealed class AnthropicTextGeneration : ITextGenerator
{
    private readonly RawAnthropicClient _client;
    private readonly string _modelName;
    private readonly ITextTokenizer _textTokenizer;
    private readonly ILogger<AnthropicTextGeneration> _log;

    public AnthropicTextGeneration(
        IHttpClientFactory httpClientFactory,
        AnthropicConfiguration config,
        ITextTokenizer? textTokenizer = null,
        ILogger<AnthropicTextGeneration>? log = null)
    {
        this._modelName = config.TextModelName;

        // Using the smallest value for now - KM support MaxTokenIn and MaxTokenOut TODO
        this.MaxTokenTotal = config.MaxTokenOut;

        this._log = log ?? DefaultLogger<AnthropicTextGeneration>.Instance;

        this._client = new RawAnthropicClient(
            config.ApiKey,
            config.Endpoint,
            config.EndpointVersion,
            httpClientFactory,
            config.HttpClientName);

        if (textTokenizer == null)
        {
            this._log.LogWarning(
                "Tokenizer not specified, will use {0}. The token count might be incorrect, causing unexpected errors",
                nameof(TikTokenTokenizer));
            textTokenizer = new TikTokenTokenizer();
        }

        this._textTokenizer = textTokenizer;
    }

    /// <inheritdoc />
    public int MaxTokenTotal { get; private set; }

    /// <inheritdoc />
    public int CountTokens(string text)
    {
        return this._textTokenizer.CountTokens(text);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> GenerateTextAsync(
        string prompt,
        TextGenerationOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        CallClaudeStreamingParams parameters = new(this._modelName, prompt)
        {
            System = "You are an assistant that will answer user query based on a context",
            Temperature = options.Temperature,
        };
        var streamedResponse = this._client.CallClaudeStreamingAsync(parameters);

        await foreach (var response in streamedResponse.WithCancellation(cancellationToken))
        {
            //now we simply yield the response
            switch (response)
            {
                case ContentBlockDelta blockDelta:
                    yield return blockDelta.Delta.Text;
                    break;

                default:
                    //do nothing we simple want to use delta text.
                    break;
            }
        }
    }
}
