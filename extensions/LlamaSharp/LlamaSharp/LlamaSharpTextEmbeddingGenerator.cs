// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LLama;
using LLama.Common;
using LLama.Native;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;

namespace Microsoft.KernelMemory.AI.LlamaSharp;

public sealed class LlamaSharpTextEmbeddingGenerator : ITextEmbeddingGenerator, IDisposable
{
    private readonly LLamaEmbedder _embedder;
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
    public LlamaSharpTextEmbeddingGenerator(
        LlamaSharpModelConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
    {
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<LlamaSharpTextGenerator>();

        config.Validate();
        this.MaxTokens = (int)config.MaxTokenTotal;

        var parameters = new ModelParams(config.ModelPath)
        {
            ContextSize = config.MaxTokenTotal,
            GpuLayerCount = config.GpuLayerCount ?? 20,
            Embeddings = true,
            PoolingType = LLamaPoolingType.None,
        };

        var modelFilename = config.ModelPath.Split('/').Last().Split('\\').Last();
        this._log.LogDebug("Loading LLama model: {1}", modelFilename);

        this._model = LLamaWeights.LoadFromFile(parameters);
        this._context = this._model.CreateContext(parameters);
        this._log.LogDebug("LLama model loaded");

        this._embedder = new LLamaEmbedder(this._model, parameters);
        this._textTokenizer = textTokenizer ?? new LlamaSharpTokenizer(this._context);
    }

    /// <inheritdoc/>
    public int MaxTokens { get; }

    /// <inheritdoc/>
    public int CountTokens(string text)
    {
        return this._textTokenizer.CountTokens(text);
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetTokens(string text)
    {
        return this._textTokenizer.GetTokens(text);
    }

    /// <inheritdoc/>
    public async Task<Embedding> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (this._log.IsEnabled(LogLevel.Trace))
        {
            this._log.LogTrace("Generating embedding, input token size: {0}, text length: {1}", this._textTokenizer.CountTokens(text), text.Length);
        }

        IReadOnlyList<float[]> embeddings = await this._embedder.GetEmbeddings(text, cancellationToken).ConfigureAwait(false);
        return new Embedding(embeddings[0]);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this._embedder.Dispose();
        this._model.Dispose();
        this._context.Dispose();
    }
}
