// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;

namespace Microsoft.KernelMemory.AI.OpenAI;

/// <summary>
/// Text embedding generator. The class can be used with any service
/// supporting OpenAI HTTP schema.
/// </summary>
[Experimental("KMEXP01")]
public sealed class OpenAITextEmbeddingGenerator : ITextEmbeddingGenerator, ITextEmbeddingBatchGenerator
{
    private readonly ITextEmbeddingGenerationService _client = null!;
    private readonly ITextTokenizer _textTokenizer;
    private readonly ILogger<OpenAITextEmbeddingGenerator> _log;
    private readonly string _model;

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
        ILoggerFactory? loggerFactory = null) : this(config, textTokenizer, loggerFactory)
    {
        this._client = new OpenAITextEmbeddingGenerationService(
            modelId: config.EmbeddingModel,
            openAIClient: openAIClient,
            dimensions: config.EmbeddingDimensions,
            loggerFactory: loggerFactory);
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
        ILoggerFactory? loggerFactory = null) : this(config, textTokenizer, loggerFactory)
    {
        this._client = skService;
    }

    /// <summary>
    /// Create new instance.
    /// </summary>
    /// <param name="config">Endpoint and model configuration</param>
    /// <param name="textTokenizer">Text tokenizer, possibly matching the model used</param>
    /// <param name="loggerFactory">App logger factory</param>
    /// <param name="httpClient">Optional HTTP client with custom settings</param>
    public OpenAITextEmbeddingGenerator(
        OpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null,
        HttpClient? httpClient = null) : this(config, textTokenizer, loggerFactory)
    {
        this._client = new OpenAITextEmbeddingGenerationService(
            modelId: config.EmbeddingModel,
            openAIClient: OpenAIClientBuilder.BuildOpenAIClient(config, httpClient),
            loggerFactory: loggerFactory,
            dimensions: config.EmbeddingDimensions);
    }

    /// <inheritdoc/>
    public int MaxTokens { get; }

    /// <inheritdoc/>
    public int MaxBatchSize { get; }

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
        this._log.LogTrace("Generating embedding, model '{0}'", this._model);
        return this._client.GenerateEmbeddingAsync(text, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Embedding[]> GenerateEmbeddingBatchAsync(IEnumerable<string> textList, CancellationToken cancellationToken = default)
    {
        var list = textList.ToList();
        this._log.LogTrace("Generating embeddings, model '{0}', batch size '{1}'", this._model, list.Count);
        var embeddings = await this._client.GenerateEmbeddingsAsync(list, cancellationToken: cancellationToken).ConfigureAwait(false);
        return embeddings.Select(e => new Embedding(e)).ToArray();
    }

    /// <summary>
    /// Internal common constructor code
    /// </summary>
    /// <param name="config">Endpoint and model configuration</param>
    /// <param name="textTokenizer">Text tokenizer, possibly matching the model used</param>
    /// <param name="loggerFactory">App logger factory</param>
    private OpenAITextEmbeddingGenerator(
        OpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
    {
        this._model = config.EmbeddingModel;
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<OpenAITextEmbeddingGenerator>();

        if (textTokenizer == null)
        {
            this._log.LogWarning(
                "Tokenizer not specified, will use {0}. The token count might be incorrect, causing unexpected errors",
                nameof(GPT4Tokenizer));
            textTokenizer = new GPT4Tokenizer();
        }

        this._textTokenizer = textTokenizer;

        this.MaxTokens = config.EmbeddingModelMaxTokenTotal;
        this.MaxBatchSize = config.MaxEmbeddingBatchSize;
    }
}
