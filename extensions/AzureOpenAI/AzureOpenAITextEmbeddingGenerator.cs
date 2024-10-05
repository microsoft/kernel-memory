// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.AzureOpenAI.Internals;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace Microsoft.KernelMemory.AI.AzureOpenAI;

[Experimental("KMEXP01")]
public sealed class AzureOpenAITextEmbeddingGenerator : ITextEmbeddingGenerator, ITextEmbeddingBatchGenerator
{
    private readonly AzureOpenAITextEmbeddingGenerationService _client;
    private readonly ITextTokenizer _textTokenizer;
    private readonly ILogger<AzureOpenAITextEmbeddingGenerator> _log;

    /// <inheritdoc/>
    public int MaxTokens { get; }

    /// <inheritdoc/>
    public int MaxBatchSize { get; }

    /// <summary>
    /// Create a new instance.
    /// </summary>
    /// <param name="config">Client and service configuration</param>
    /// <param name="textTokenizer">Text tokenizer, possibly matching the model used</param>
    /// <param name="loggerFactory">App logger factory</param>
    /// <param name="httpClient">Optional HTTP client with custom settings</param>
    public AzureOpenAITextEmbeddingGenerator(
        AzureOpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null,
        HttpClient? httpClient = null)
        : this(
            config,
            AzureOpenAIClientBuilder.Build(config, httpClient, loggerFactory),
            textTokenizer,
            loggerFactory)
    {
    }

    /// <summary>
    /// Create a new instance.
    /// </summary>
    /// <param name="config">Client and service configuration</param>
    /// <param name="azureClient">Azure OpenAI client with custom settings</param>
    /// <param name="textTokenizer">Text tokenizer, possibly matching the model used</param>
    /// <param name="loggerFactory">App logger factory</param>
    public AzureOpenAITextEmbeddingGenerator(
        AzureOpenAIConfig config,
        AzureOpenAIClient azureClient,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
        : this(
            config,
            SkClientBuilder.BuildEmbeddingClient(config.Deployment, azureClient, config.EmbeddingDimensions, loggerFactory),
            textTokenizer,
            loggerFactory)
    {
    }

    /// <summary>
    /// Create a new instance.
    /// </summary>
    /// <param name="config"></param>
    /// <param name="skClient"></param>
    /// <param name="textTokenizer"></param>
    /// <param name="loggerFactory"></param>
    public AzureOpenAITextEmbeddingGenerator(
        AzureOpenAIConfig config,
        AzureOpenAITextEmbeddingGenerationService skClient,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
    {
        this._client = skClient;
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<AzureOpenAITextEmbeddingGenerator>();
        this.MaxTokens = config.MaxTokenTotal;
        this.MaxBatchSize = config.MaxEmbeddingBatchSize;

        if (textTokenizer == null)
        {
            this._log.LogWarning(
                "Tokenizer not specified, will use {0}. The token count might be incorrect, causing unexpected errors",
                nameof(GPT4oTokenizer));
            textTokenizer = new GPT4oTokenizer();
        }

        this._textTokenizer = textTokenizer;
    }

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
    public Task<Embedding> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        this._log.LogTrace("Generating embedding");
        return this._client.GenerateEmbeddingAsync(text, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Embedding[]> GenerateEmbeddingBatchAsync(IEnumerable<string> textList, CancellationToken cancellationToken = default)
    {
        var list = textList.ToList();
        this._log.LogTrace("Generating embeddings, batch size '{0}'", list.Count);
        IList<ReadOnlyMemory<float>> embeddings = await this._client.GenerateEmbeddingsAsync(list, cancellationToken: cancellationToken).ConfigureAwait(false);
        return embeddings.Select(e => new Embedding(e)).ToArray();
    }
}
