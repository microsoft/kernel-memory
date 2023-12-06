// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.TextGeneration;

namespace Microsoft.KernelMemory.AI;
internal class SemanticKernelTextGeneration : ITextGeneration
{
    private readonly ITextGenerationService _completion;

    public SemanticKernelTextGeneration(ITextGenerationService completion)
    {
        this._completion = completion;
    }

    public async IAsyncEnumerable<string> GenerateTextAsync(string prompt, TextGenerationOptions options, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var contents = this._completion.GetStreamingTextContentsAsync(prompt, this.ToAIRequestSettings(options), null, cancellationToken);
        await foreach (var content in contents)
        {
            if (content != null)
            {
                yield return content.ToString();
            }
        }
    }

    private PromptExecutionSettings ToAIRequestSettings(TextGenerationOptions options)
    {
        var settings = new PromptExecutionSettings();
        settings.ExtensionData[nameof(options.Temperature)] = options.Temperature;
        settings.ExtensionData[nameof(options.TopP)] = options.TopP;
        settings.ExtensionData[nameof(options.PresencePenalty)] = options.PresencePenalty;
        settings.ExtensionData[nameof(options.FrequencyPenalty)] = options.FrequencyPenalty;
        settings.ExtensionData[nameof(options.StopSequences)] = options.StopSequences;

        if (options.MaxTokens != null)
        {
            settings.ExtensionData[nameof(options.MaxTokens)] = options.MaxTokens;
        }

        settings.ExtensionData[nameof(options.ResultsPerPrompt)] = options.ResultsPerPrompt;
        settings.ExtensionData[nameof(options.TokenSelectionBiases)] = options.TokenSelectionBiases;
        return settings;
    }
}
