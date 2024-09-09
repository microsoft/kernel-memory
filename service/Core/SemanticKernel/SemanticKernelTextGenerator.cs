// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;

namespace Microsoft.KernelMemory.SemanticKernel;

internal sealed class SemanticKernelTextGenerator : ITextGenerator
{
    private readonly ITextGenerationService _service;
    private readonly ITextTokenizer _tokenizer;
    private readonly ILogger<SemanticKernelTextGenerator> _log;

    /// <inheritdoc />
    public int MaxTokenTotal { get; }

    /// <inheritdoc />
    public int CountTokens(string text) => this._tokenizer.CountTokens(text);

    /// <inheritdoc />
    public IReadOnlyList<string> GetTokens(string text) => this._tokenizer.GetTokens(text);

    public SemanticKernelTextGenerator(
        ITextGenerationService textGenerationService,
        SemanticKernelConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullExceptionEx.ThrowIfNull(textGenerationService, nameof(textGenerationService), "Text generation service is null");

        this._service = textGenerationService;
        this.MaxTokenTotal = config.MaxTokenTotal;

        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<SemanticKernelTextGenerator>();

        if (textTokenizer == null)
        {
            this._log.LogWarning(
                "Tokenizer not specified, will use {0}. The token count might be incorrect, causing unexpected errors",
                nameof(GPT4oTokenizer));
            textTokenizer = new GPT4oTokenizer();
        }

        this._tokenizer = textTokenizer;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> GenerateTextAsync(
        string prompt,
        TextGenerationOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        this._log.LogTrace("Generating text with SK text generator service");

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
            ExtensionData = new Dictionary<string, object>
            {
                ["temperature"] = options.Temperature,
                ["top_p"] = options.NucleusSampling,
                ["presence_penalty"] = options.PresencePenalty,
                ["frequency_penalty"] = options.FrequencyPenalty,
                ["stop_sequences"] = options.StopSequences,
                ["results_per_prompt"] = options.ResultsPerPrompt,
                ["token_selection_biases"] = options.TokenSelectionBiases
            }
        };

        if (options.MaxTokens != null)
        {
            settings.ExtensionData["max_tokens"] = options.MaxTokens;
        }

        return settings;
    }
}
