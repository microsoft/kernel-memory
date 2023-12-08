// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LLama;
using LLama.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.KernelMemory.AI.Llama;

public class LlamaSharpTextGenerator : ITextEmbeddingGenerator, IDisposable
{
    private readonly LLamaWeights _model;

    public LlamaSharpTextGenerator(
        LlamaSharpConfig config,
        ILoggerFactory? loggerFactory = null)
        : this(config, loggerFactory?.CreateLogger<LlamaSharpTextGenerator>())
    {
    }

    public LlamaSharpTextGenerator(
        LlamaSharpConfig config,
        ILogger<LlamaSharpTextGenerator>? log = null)
    {
        this.MaxTokens = (int)config.MaxTokenTotal;

        var parameters = new ModelParams(config.ModelPath)
        {
            GpuLayerCount = config.GpuLayerCount,
            ContextSize = config.MaxTokenTotal,
            Seed = config.Seed,
        };

        this._model = LLamaWeights.LoadFromFile(parameters);
    }

    /// <inheritdoc/>
    public int MaxTokens { get; }

    /// <inheritdoc/>
    public int CountTokens(string text)
    {
        return this._model.NativeHandle
            .Tokenize(text, add_bos: false, special: false, Encoding.GetEncoding(text)).Length;
    }

    /// <inheritdoc/>
    public Task<Embedding> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this._model.Dispose();
    }
}
