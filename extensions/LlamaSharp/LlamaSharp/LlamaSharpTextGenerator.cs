// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using LLama;
using LLama.Abstractions;
using LLama.Common;
using LLama.Sampling;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Diagnostics;

namespace Microsoft.KernelMemory.AI.LlamaSharp;

/// <summary>
/// Text generator based on LLama models, via LLamaSharp / llama.cpp
/// See https://github.com/SciSharp/LLamaSharp
/// </summary>
[Experimental("KMEXP01")]
public sealed class LlamaSharpTextGenerator : ITextGenerator, IDisposable
{
    private readonly LLamaWeights _model;
    private readonly LLamaContext _context;
    private readonly ITextTokenizer _textTokenizer;
    private readonly ILogger<LlamaSharpTextGenerator> _log;

    /// <summary>
    /// Create new instance
    /// </summary>
    /// <param name="config">Configuration settings</param>
    /// <param name="textTokenizer">Optional text tokenizer, replacing the one provided by the model</param>
    /// <param name="loggerFactory">Application logger instance</param>
    public LlamaSharpTextGenerator(
        LlamaSharpConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
    {
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<LlamaSharpTextGenerator>();

        config.Validate();
        this.MaxTokenTotal = (int)config.MaxTokenTotal;

        if (textTokenizer == null)
        {
            this._log.LogWarning(
                "Tokenizer not specified, will use {0}. The token count might be incorrect, causing unexpected errors",
                nameof(GPT4Tokenizer));
            textTokenizer = new GPT4Tokenizer();
        }

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
        int? value = this._textTokenizer?.CountTokens(text);
        if (!value.HasValue)
        {
            value = this._context.Tokenize(text, addBos: false, special: false).Length;
        }

        return value.Value;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetTokens(string text)
    {
        return this._textTokenizer.GetTokens(text);
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<string> GenerateTextAsync(
        string prompt,
        TextGenerationOptions options,
        CancellationToken cancellationToken = default)
    {
        var executor = new InteractiveExecutor(this._context);

        var samplingPipeline = new DefaultSamplingPipeline();
        samplingPipeline.Temperature = (float)options.Temperature;
        samplingPipeline.TopP = (float)options.NucleusSampling;
        samplingPipeline.AlphaPresence = (float)options.PresencePenalty;
        samplingPipeline.AlphaFrequency = (float)options.FrequencyPenalty;

        if (options.TokenSelectionBiases is { Count: > 0 })
        {
            foreach (var (token, bias) in options.TokenSelectionBiases)
            {
                samplingPipeline.LogitBias!.Add(token, bias);
            }
        }

        IInferenceParams settings = new InferenceParams
        {
            TokensKeep = this.MaxTokenTotal,
            MaxTokens = options.MaxTokens ?? -1,
            AntiPrompts = options.StopSequences?.ToList() ?? new(),
            SamplingPipeline = samplingPipeline
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
