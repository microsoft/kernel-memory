// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.TextCompletion;

namespace Microsoft.KernelMemory.AI;
internal class SemanticKernelTextGeneration : ITextGeneration
{
    private readonly ITextCompletion _completion;

    public SemanticKernelTextGeneration(ITextCompletion completion)
    {
        this._completion = completion;
    }

    public IAsyncEnumerable<string> GenerateTextAsync(string prompt, TextGenerationOptions options, CancellationToken cancellationToken = default)
    {
        return this._completion.CompleteStreamAsync(prompt, this.ToAIRequestSettings(options), cancellationToken);
    }

    private AIRequestSettings ToAIRequestSettings(TextGenerationOptions options)
    {
        var settings = new AIRequestSettings();
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
