// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Embeddings;

namespace Microsoft.KernelMemory.SemanticKernel;

internal class SemanticKernelTextEmbeddingGenerator : ITextEmbeddingGenerator
{
    private readonly ITextEmbeddingGenerationService _service;
    private readonly ITextTokenizer _tokenizer;
    private readonly ILogger<SemanticKernelTextEmbeddingGenerator> _log;

    /// <inheritdoc />
    public int MaxTokens { get; }

    /// <inheritdoc />
    public int CountTokens(string text) => this._tokenizer.CountTokens(text);

    public SemanticKernelTextEmbeddingGenerator(
        ITextEmbeddingGenerationService textEmbeddingGenerationService,
        SemanticKernelConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
    {
        this._service = textEmbeddingGenerationService ?? throw new ArgumentNullException(nameof(textEmbeddingGenerationService));
        this.MaxTokens = config.MaxTokenTotal;

        var log = loggerFactory?.CreateLogger<SemanticKernelTextEmbeddingGenerator>();
        this._log = log ?? DefaultLogger<SemanticKernelTextEmbeddingGenerator>.Instance;

        if (textTokenizer == null)
        {
            this._log.LogWarning(
                "Tokenizer not specified, will use {0}. The token count might be incorrect, causing unexpected errors",
                nameof(DefaultGPTTokenizer));
            textTokenizer = new DefaultGPTTokenizer();
        }

        this._tokenizer = textTokenizer;
    }

    /// <inheritdoc />
    public Task<Embedding> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        this._log.LogTrace("Generating embedding with SK embedding generator service");

        return this._service.GenerateEmbeddingAsync(text, cancellationToken);
    }
}
