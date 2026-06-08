// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;

namespace Microsoft.KernelMemory.AI;

/// <summary>
/// Disabled embedding generator used when using KM without embeddings,
/// e.g. when using the internal orchestration to run jobs that don't require AI.
/// </summary>
public class NoEmbeddingGenerator : ITextEmbeddingGenerator
{
    private readonly ILogger<NoEmbeddingGenerator> _log;

    public NoEmbeddingGenerator(ILoggerFactory? loggerFactory = null)
    {
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<NoEmbeddingGenerator>();
    }

    /// <inheritdoc />
    public int MaxTokens => int.MaxValue;

    /// <inheritdoc />
    public int CountTokens(string text)
    {
        throw this.Error();
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetTokens(string text)
    {
        throw this.Error();
    }

    /// <inheritdoc />
    public Task<Embedding> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        throw this.Error();
    }

    private NotImplementedException Error()
    {
        this._log.LogCritical("The application is attempting to generate embeddings even if embedding generation has been disabled");
        return new NotImplementedException("Embedding generation has been disabled");
    }
}
