// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.KernelMemory.AI;

/// <summary>
/// Disabled embedding generator used when using KM without embeddings,
/// e.g. when using the internal orchestration to run jobs that don't require AI.
/// </summary>
public class NoEmbeddingGenerator : ITextEmbeddingGenerator
{
    private readonly ILogger<ITextEmbeddingGenerator> _log;

    public NoEmbeddingGenerator(ILoggerFactory loggerFactory)
    {
        this._log = loggerFactory.CreateLogger<ITextEmbeddingGenerator>();
    }

    /// <inheritdoc />
    public int MaxTokens => int.MaxValue;

    /// <inheritdoc />
    public int CountTokens(string text)
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
