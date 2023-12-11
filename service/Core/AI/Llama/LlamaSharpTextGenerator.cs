// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LLama;
using LLama.Abstractions;
using LLama.Common;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;

namespace Microsoft.KernelMemory.AI.Llama;

/// <summary>
/// Text generator based on LLama models, via LLamaSharp / llama.cpp
/// See https://github.com/SciSharp/LLamaSharp
/// </summary>
public sealed class LlamaSharpTextGenerator : ITextGenerator, IDisposable
{
    private readonly LLamaWeights _model;
    private readonly LLamaContext _context;
    private readonly ITextTokenizer? _textTokenizer;
    private readonly ILogger<LlamaSharpTextGenerator> _log;

    /// <summary>
    /// Create new instance
    /// </summary>
    /// <param name="config">Configuration settings</param>
    /// <param name="textTokenizer">Optional text tokenizer, replacing the one provided by the model</param>
    /// <param name="loggerFactory">Optional .NET logger factory</param>
    public LlamaSharpTextGenerator(
        LlamaSharpConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
        : this(config, textTokenizer, loggerFactory?.CreateLogger<LlamaSharpTextGenerator>())
    {
    }

    /// <summary>
    /// Create new instance
    /// </summary>
    /// <param name="config">Configuration settings</param>
    /// <param name="textTokenizer">Optional text tokenizer, replacing the one provided by the model</param>
    /// <param name="log">Logger instance</param>
    public LlamaSharpTextGenerator(
        LlamaSharpConfig config,
        ITextTokenizer? textTokenizer = null,
        ILogger<LlamaSharpTextGenerator>? log = null)
    {
        this._log = log ?? DefaultLogger<LlamaSharpTextGenerator>.Instance;

        config.Validate();
        this.MaxTokenTotal = (int)config.MaxTokenTotal;
        this._textTokenizer = textTokenizer;

        var parameters = new ModelParams(config.ModelPath)
        {
            ContextSize = config.MaxTokenTotal
        };

        if (config.GpuLayerCount.HasValue)
        {
            parameters.GpuLayerCount = config.GpuLayerCount.Value;
        }

        if (config.Seed.HasValue)
        {
            parameters.Seed = config.Seed.Value;
        }

        var modelFilename = config.ModelPath.Split('/').Last().Split('\\').Last();
        this._log.LogDebug("Loading LLama model: {1}", modelFilename);
        this._model = LLamaWeights.LoadFromFile(parameters);
        this._context = this._model.CreateContext(parameters);
        this._log.LogDebug("LLama model loaded");
    }

    /// <inheritdoc/>
    public int MaxTokenTotal { get; }

    /// <inheritdoc/>
    public int CountTokens(string text)
    {
        return this._textTokenizer?.CountTokens(text)
               ?? this._context.Tokenize(text, addBos: false, special: false).Length;
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<string> GenerateTextAsync(
        string prompt,
        TextGenerationOptions options,
        CancellationToken cancellationToken = default)
    {
        var executor = new InteractiveExecutor(this._context);
        IInferenceParams settings = new InferenceParams
        {
            TokensKeep = this.MaxTokenTotal,
            MaxTokens = options.MaxTokens ?? -1,
            Temperature = (float)options.Temperature,
            TopP = (float)options.TopP,
            PresencePenalty = (float)options.PresencePenalty,
            FrequencyPenalty = (float)options.FrequencyPenalty,
            AntiPrompts = options.StopSequences.ToList(),
            LogitBias = options.TokenSelectionBiases,
            // RepeatLastTokensCount = 0, // [int] last n tokens to penalize (0 = disable penalty, -1 = context size)
            // TopK = 0, // [int] The number of highest probability vocabulary tokens to keep for top-k-filtering.
            // MinP = 0, // [float]
            // TfsZ = 0, // [float]
            // TypicalP = 0, // [float]
            // RepeatPenalty = 0, // [float]
            // MirostatTau = 0, // [float]
            // MirostatEta = 0, // [float]
            // PenalizeNL = false, // consider newlines as a repeatable token
            // Mirostat = MirostatType.Disable, // see https://github.com/basusourya/mirostat
            // Grammar = null // SafeLLamaGrammarHandle
        };
        return executor.InferAsync(prompt, settings, cancellationToken);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing) { return; }

        this._context.Dispose();
        this._model.Dispose();
    }

    ~LlamaSharpTextGenerator()
    {
        this.Dispose(false);
    }
}
