// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.TextGeneration;

namespace Microsoft.KernelMemory.AI;

internal class SemanticKernelTextGenerator : ITextGenerator
{
    private readonly ITextGenerationService _service;
    private readonly ITextTokenizer _tokenizer;
    private readonly SemanticKernelConfig _config;

    public int MaxTokenTotal { get; }

    public SemanticKernelTextGenerator(ITextGenerationService service, SemanticKernelConfig config, ITextTokenizer tokenizer)
    {
        this._service = service ?? throw new ArgumentNullException(nameof(service));
        this._tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer), "Tokenizer not specified. The token count might be incorrect, causing unexpected errors");
        this._config = config;
        this.MaxTokenTotal = config.MaxTokenTotal;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> GenerateTextAsync(
        string prompt,
        TextGenerationOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var contents = this._service.GetStreamingTextContentsAsync(
            prompt, ToPromptExecutionSettings(options), null, cancellationToken).ConfigureAwait(false);

        await foreach (StreamingTextContent? content in contents)
        {
            if (content != null)
            {
                yield return content.ToString();
            }
        }
    }

    private static PromptExecutionSettings ToPromptExecutionSettings(TextGenerationOptions options)
    {
        var settings = new PromptExecutionSettings
        {
            ExtensionData =
            {
                [nameof(options.Temperature)] = options.Temperature,
                [nameof(options.TopP)] = options.TopP,
                [nameof(options.PresencePenalty)] = options.PresencePenalty,
                [nameof(options.FrequencyPenalty)] = options.FrequencyPenalty,
                [nameof(options.StopSequences)] = options.StopSequences,
                [nameof(options.ResultsPerPrompt)] = options.ResultsPerPrompt,
                [nameof(options.TokenSelectionBiases)] = options.TokenSelectionBiases
            }
        };

        if (options.MaxTokens != null)
        {
            settings.ExtensionData[nameof(options.MaxTokens)] = options.MaxTokens;
        }

        return settings;
    }

    /// <inheritdoc />
    public int CountTokens(string text) => this._tokenizer.CountTokens(text);
}
