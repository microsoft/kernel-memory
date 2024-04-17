// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.KernelMemory.AI.Anthropic.Client;
using TiktokenSharp;

namespace Microsoft.KernelMemory.AI.Anthropic;

internal sealed class AnthropicTextGeneration : ITextGenerator
{
    private static readonly TikToken s_tokenizer = TikToken.GetEncoding("cl100k_base");

    private readonly RawAnthropicClient _client;
    private readonly string _modelName;

    public AnthropicTextGeneration(
        IHttpClientFactory httpClientFactory,
        AnthropicConfiguration config)
    {
        this._modelName = config.TextModelName;
        this.MaxTokenTotal = config.MaxTokenTotal;

        this._client = new RawAnthropicClient(
            config.ApiKey,
            config.Endpoint,
            config.EndpointVersion,
            httpClientFactory,
            config.HttpClientName);
    }

    /// <inheritdoc />
    public int MaxTokenTotal { get; private set; }

    /// <inheritdoc />
    public int CountTokens(string text)
    {
        return s_tokenizer.Encode(text).Count;
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
