// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.OpenAI.Internals;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Embeddings;
using OpenAI;

namespace Microsoft.KernelMemory.AI.OpenAI;

/// <summary>
/// Text embedding generator. The class can be used with any service
/// supporting OpenAI HTTP schema.
///
/// Note: does not support model name override via request context
///       see https://github.com/microsoft/semantic-kernel/issues/9337
/// </summary>
[Experimental("KMEXP01")]
public sealed class OpenAITextEmbeddingGenerator : ITextEmbeddingGenerator, ITextEmbeddingBatchGenerator
{
    private readonly ITextEmbeddingGenerationService _client;
    private readonly ITextTokenizer _textTokenizer;
    private readonly ILogger<OpenAITextEmbeddingGenerator> _log;

    /// <inheritdoc/>
    public int MaxTokens { get; }

    /// <inheritdoc/>
    public int MaxBatchSize { get; }

    /// <summary>
    /// Create a new instance.
    /// </summary>
    /// <param name="config">Client and model configuration</param>
    /// <param name="textTokenizer">Text tokenizer, possibly matching the model used</param>
    /// <param name="loggerFactory">App logger factory</param>
    /// <param name="httpClient">Optional HTTP client with custom settings</param>
    public OpenAITextEmbeddingGenerator(
        OpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null,
        HttpClient? httpClient = null)
        : this(
            config,
            OpenAIClientBuilder.Build(config, httpClient, loggerFactory),
            textTokenizer,
            loggerFactory)
    {
    }

    /// <summary>
    /// Create a new instance, using the given OpenAI pre-configured client.
    /// This constructor allows to have complete control on the OpenAI client definition.
    /// </summary>
    /// <param name="config">Model configuration</param>
    /// <param name="openAIClient">Custom OpenAI client, already configured</param>
    /// <param name="textTokenizer">Text tokenizer, possibly matching the model used</param>
    /// <param name="loggerFactory">App logger factory</param>
    public OpenAITextEmbeddingGenerator(
        OpenAIConfig config,
        OpenAIClient openAIClient,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
        : this(
            config,
            SkClientBuilder.BuildEmbeddingClient(config.EmbeddingModel, openAIClient, config.EmbeddingDimensions, loggerFactory),
            textTokenizer,
            loggerFactory)
    {
    }

    /// <summary>
    /// Create a new instance, using the given SK Embedding service.
    /// This constructor allows to easily reuse SK embedding service definitions.
    /// </summary>
    /// <param name="config">Model configuration</param>
    /// <param name="skService">SK embedding service</param>
    /// <param name="textTokenizer">Text tokenizer, possibly matching the model used</param>
    /// <param name="loggerFactory">App logger factory</param>
    public OpenAITextEmbeddingGenerator(
        OpenAIConfig config,
        ITextEmbeddingGenerationService skService,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
    {
        this._client = skService;
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<OpenAITextEmbeddingGenerator>();
        this.MaxTokens = config.EmbeddingModelMaxTokenTotal;
        this.MaxBatchSize = config.MaxEmbeddingBatchSize;

        if (textTokenizer == null && !string.IsNullOrEmpty(config.EmbeddingModelTokenizer))
        {
            textTokenizer = TokenizerFactory.GetTokenizerForEncoding(config.EmbeddingModelTokenizer);
        }

        textTokenizer ??= TokenizerFactory.GetTokenizerForModel(config.EmbeddingModel);
        if (textTokenizer == null)
        {
            textTokenizer = new CL100KTokenizer();
            this._log.LogWarning(
                "Tokenizer not specified, will use {0}. The token count might be incorrect, causing unexpected errors",
                textTokenizer.GetType().FullName);
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
        try
        {
            return this._client.GenerateEmbeddingAsync(text, cancellationToken);
        }
        catch (HttpOperationException e)
        {
            throw new OpenAIException(e.Message, e, isTransient: e.StatusCode.IsTransientError());
        }
    }

    /// <inheritdoc/>
    public async Task<Embedding[]> GenerateEmbeddingBatchAsync(IEnumerable<string> textList, CancellationToken cancellationToken = default)
    {
        var list = textList.ToList();
        this._log.LogTrace("Generating embeddings, batch size '{0}'", list.Count);
        try
        {
            var embeddings = await this._client.GenerateEmbeddingsAsync(list, cancellationToken: cancellationToken).ConfigureAwait(false);
            return embeddings.Select(e => new Embedding(e)).ToArray();
        }
        catch (HttpOperationException e)
        {
            throw new OpenAIException(e.Message, e, isTransient: e.StatusCode.IsTransientError());
        }
    }
}
