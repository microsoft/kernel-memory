// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Microsoft.KernelMemory.AI.OpenAI;

public class OpenAITextEmbeddingGenerator : ITextEmbeddingGenerator
{
    private readonly ITextTokenizer _textTokenizer;
    private readonly OpenAITextEmbeddingGenerationService _client;
    private readonly ILogger<OpenAITextEmbeddingGenerator> _log;

    public OpenAITextEmbeddingGenerator(
        OpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null,
        HttpClient? httpClient = null)
        : this(config, textTokenizer, loggerFactory?.CreateLogger<OpenAITextEmbeddingGenerator>(), httpClient)
    {
    }

    public OpenAITextEmbeddingGenerator(
        OpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        ILogger<OpenAITextEmbeddingGenerator>? log = null,
        HttpClient? httpClient = null)
    {
        this._log = log ?? DefaultLogger<OpenAITextEmbeddingGenerator>.Instance;

        if (textTokenizer == null)
        {
            this._log.LogWarning(
                "Tokenizer not specified, will use {0}. The token count might be incorrect, causing unexpected errors",
                nameof(DefaultGPTTokenizer));
            textTokenizer = new DefaultGPTTokenizer();
        }

        this._textTokenizer = textTokenizer;

        this.MaxTokens = config.EmbeddingModelMaxTokenTotal;

        this._client = new OpenAITextEmbeddingGenerationService(
            modelId: config.EmbeddingModel,
            apiKey: config.APIKey,
            organization: config.OrgId,
            httpClient: httpClient);
    }

    /// <inheritdoc/>
    public int MaxTokens { get; }

    /// <inheritdoc/>
    public int CountTokens(string text)
    {
        return this._textTokenizer.CountTokens(text);
    }

    /// <inheritdoc/>
    public Task<Embedding> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        return this._client.GenerateEmbeddingAsync(text, cancellationToken);
    }
}
