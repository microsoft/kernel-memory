// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Embeddings;

namespace Microsoft.KernelMemory.SemanticKernel;

public sealed class SemanticKernelTextEmbeddingGenerator : ITextEmbeddingGenerator
{
    private readonly ITextEmbeddingGenerationService _service;
    private readonly ITextTokenizer _tokenizer;
    private readonly ILogger<SemanticKernelTextEmbeddingGenerator> _log;

    /// <inheritdoc />
    public int MaxTokens { get; }

    /// <inheritdoc />
    public int CountTokens(string text) => this._tokenizer.CountTokens(text);

    /// <inheritdoc />
    public IReadOnlyList<string> GetTokens(string text) => this._tokenizer.GetTokens(text);

    public SemanticKernelTextEmbeddingGenerator(
        ITextEmbeddingGenerationService textEmbeddingGenerationService,
        SemanticKernelConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullExceptionEx.ThrowIfNull(textEmbeddingGenerationService, nameof(textEmbeddingGenerationService), "Embedding generation service is null");

        this._service = textEmbeddingGenerationService;
        this.MaxTokens = config.MaxTokenTotal;

        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<SemanticKernelTextEmbeddingGenerator>();

        if (textTokenizer == null)
        {
            textTokenizer = new CL100KTokenizer();
            this._log.LogWarning(
                "Tokenizer not specified, will use {0}. The token count might be incorrect, causing unexpected errors",
                textTokenizer.GetType().FullName);
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
