// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
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
public sealed class OpenAITextEmbeddingGenerator : ITextEmbeddingGenerator
{
    private readonly ITextEmbeddingGenerationService _client;
    private readonly ILogger<OpenAITextEmbeddingGenerator> _log;
    private ITextTokenizer? _textTokenizer;

    /// <inheritdoc/>
    public int MaxTokens { get; }

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
    {
        this._log = loggerFactory?.CreateLogger<OpenAITextEmbeddingGenerator>()
                    ?? DefaultLogger<OpenAITextEmbeddingGenerator>.Instance;

        this.SetTokenizer(textTokenizer);
        this.MaxTokens = config.EmbeddingModelMaxTokenTotal;

        this._client = new OpenAITextEmbeddingGenerationService(
            modelId: config.EmbeddingModel,
            openAIClient: openAIClient,
            loggerFactory: loggerFactory,
            dimensions: config.EmbeddingDimensions);
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
        this._log = loggerFactory?.CreateLogger<OpenAITextEmbeddingGenerator>()
                    ?? DefaultLogger<OpenAITextEmbeddingGenerator>.Instance;

        this.SetTokenizer(textTokenizer);
        this.MaxTokens = config.EmbeddingModelMaxTokenTotal;

        this._client = skService;
    }

    /// <summary>
    /// Create new instance.
    /// This constructor passes the given logger factory to the internal SK service.
    /// </summary>
    /// <param name="config">Endpoint and model configuration</param>
    /// <param name="textTokenizer">Text tokenizer, possibly matching the model used</param>
    /// <param name="loggerFactory">App logger factory</param>
    /// <param name="httpClient">Optional HTTP client with custom settings</param>
    public OpenAITextEmbeddingGenerator(
        OpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null,
        HttpClient? httpClient = null)
    {
        this._log = loggerFactory?.CreateLogger<OpenAITextEmbeddingGenerator>()
                    ?? DefaultLogger<OpenAITextEmbeddingGenerator>.Instance;

        this.SetTokenizer(textTokenizer);
        this.MaxTokens = config.EmbeddingModelMaxTokenTotal;

        this._client = new OpenAITextEmbeddingGenerationService(
            modelId: config.EmbeddingModel,
            openAIClient: OpenAIClientBuilder.BuildOpenAIClient(config, httpClient),
            loggerFactory: loggerFactory,
            dimensions: config.EmbeddingDimensions);
    }

    /// <summary>
    /// Create new instance.
    /// This constructor does not pass the given logger to the internal SK service.
    /// </summary>
    /// <param name="config">Endpoint and model configuration</param>
    /// <param name="textTokenizer">Text tokenizer, possibly matching the model used</param>
    /// <param name="log">Application logger</param>
    /// <param name="httpClient">Optional HTTP client with custom settings</param>
    public OpenAITextEmbeddingGenerator(
        OpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        ILogger<OpenAITextEmbeddingGenerator>? log = null,
        HttpClient? httpClient = null)
    {
        this._log = log ?? DefaultLogger<OpenAITextEmbeddingGenerator>.Instance;

        this.SetTokenizer(textTokenizer);
        this.MaxTokens = config.EmbeddingModelMaxTokenTotal;

        this._client = new OpenAITextEmbeddingGenerationService(
            modelId: config.EmbeddingModel,
            openAIClient: OpenAIClientBuilder.BuildOpenAIClient(config, httpClient),
            dimensions: config.EmbeddingDimensions);
    }

    /// <inheritdoc/>
    public int CountTokens(string text)
    {
        return this._textTokenizer!.CountTokens(text);
    }

    /// <inheritdoc/>
    public Task<Embedding> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        return this._client.GenerateEmbeddingAsync(text, cancellationToken);
    }

    private void SetTokenizer(ITextTokenizer? textTokenizer = null)
    {
        if (textTokenizer == null)
        {
            this._log.LogWarning(
                "Tokenizer not specified, will use {0}. The token count might be incorrect, causing unexpected errors",
                nameof(DefaultGPTTokenizer));
            textTokenizer = new DefaultGPTTokenizer();
        }

        this._textTokenizer = textTokenizer;
    }
}
