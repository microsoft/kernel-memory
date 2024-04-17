// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SemanticMemory.Extensions.Anthropic;
using TiktokenSharp;

namespace Microsoft.KernelMemory.AI.Anthropic;

internal sealed class AnthropicTextGeneration : ITextGenerator
{
    private readonly AnthropicTextGenerationConfiguration _config;
    private readonly RawAnthropicClient _client;

    private static readonly TikToken s_tokenizer = TikToken.GetEncoding("cl100k_base");

    public AnthropicTextGeneration(
        IHttpClientFactory httpClientFactory,
        AnthropicTextGenerationConfiguration config)
    {
        this._config = config;
        this._client = new RawAnthropicClient(this._config.ApiKey, httpClientFactory, this._config.HttpClientName);
    }

    /// <inheritdoc />
    public int MaxTokenTotal => this._config.MaxTokenTotal;

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
        CallClaudeStreamingParams parameters = new(this._config.ModelName, prompt)
        {
            ModelName = this._config.ModelName,
            Temperature = options.Temperature,
            System = "You are an assistant that will answer user query based on a context"
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
