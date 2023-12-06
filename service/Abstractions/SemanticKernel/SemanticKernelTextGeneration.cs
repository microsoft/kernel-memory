// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.TextGeneration;

namespace Microsoft.KernelMemory.AI;

internal class SemanticKernelTextGeneration : ITextGeneration
{
    private readonly ITextGenerationService _service;

    public SemanticKernelTextGeneration(ITextGenerationService service)
    {
        this._service = service;
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
                [nameof(options.StopSequences)] = options.StopSequences
            }
        };

        if (options.MaxTokens != null)
        {
            settings.ExtensionData[nameof(options.MaxTokens)] = options.MaxTokens;
        }

        settings.ExtensionData[nameof(options.ResultsPerPrompt)] = options.ResultsPerPrompt;
        settings.ExtensionData[nameof(options.TokenSelectionBiases)] = options.TokenSelectionBiases;

        return settings;
    }
}
