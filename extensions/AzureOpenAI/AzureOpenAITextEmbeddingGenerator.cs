// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Microsoft.KernelMemory.AI.AzureOpenAI;

[Experimental("KMEXP01")]
public sealed class AzureOpenAITextEmbeddingGenerator : ITextEmbeddingGenerator, ITextEmbeddingBatchGenerator
{
    private readonly ITextTokenizer _textTokenizer;
    private readonly AzureOpenAITextEmbeddingGenerationService _client;
    private readonly ILogger<AzureOpenAITextEmbeddingGenerator> _log;
    private readonly string _deployment;

    public AzureOpenAITextEmbeddingGenerator(
        AzureOpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null,
        HttpClient? httpClient = null)
    {
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<AzureOpenAITextEmbeddingGenerator>();

        if (textTokenizer == null)
        {
            this._log.LogWarning(
                "Tokenizer not specified, will use {0}. The token count might be incorrect, causing unexpected errors",
                nameof(GPT4Tokenizer));
            textTokenizer = new GPT4Tokenizer();
        }

        this._textTokenizer = textTokenizer;
        this._deployment = config.Deployment;

        this.MaxTokens = config.MaxTokenTotal;
        this.MaxBatchSize = config.MaxEmbeddingBatchSize;

        switch (config.Auth)
        {
            case AzureOpenAIConfig.AuthTypes.AzureIdentity:
                this._client = new AzureOpenAITextEmbeddingGenerationService(
                    deploymentName: config.Deployment,
                    endpoint: config.Endpoint,
                    credential: new DefaultAzureCredential(),
                    modelId: config.Deployment,
                    httpClient: httpClient,
                    dimensions: config.EmbeddingDimensions,
                    loggerFactory: loggerFactory);
                break;

            case AzureOpenAIConfig.AuthTypes.ManualTokenCredential:
                this._client = new AzureOpenAITextEmbeddingGenerationService(
                    deploymentName: config.Deployment,
                    endpoint: config.Endpoint,
                    credential: config.GetTokenCredential(),
                    modelId: config.Deployment,
                    httpClient: httpClient,
                    dimensions: config.EmbeddingDimensions,
                    loggerFactory: loggerFactory);
                break;

            case AzureOpenAIConfig.AuthTypes.APIKey:
                this._client = new AzureOpenAITextEmbeddingGenerationService(
                    deploymentName: config.Deployment,
                    endpoint: config.Endpoint,
                    apiKey: config.APIKey,
                    modelId: config.Deployment,
                    httpClient: httpClient,
                    dimensions: config.EmbeddingDimensions,
                    loggerFactory: loggerFactory);
                break;

            default:
                throw new NotImplementedException($"Azure OpenAI auth type '{config.Auth}' not available");
        }
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
        this._log.LogTrace("Generating embedding, deployment '{0}'", this._deployment);
        return this._client.GenerateEmbeddingAsync(text, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Embedding[]> GenerateEmbeddingBatchAsync(IEnumerable<string> textList, CancellationToken cancellationToken = default)
    {
        var list = textList.ToList();
        this._log.LogTrace("Generating embeddings, deployment '{0}', batch size '{1}'", this._deployment, list.Count);
        IList<ReadOnlyMemory<float>> embeddings = await this._client.GenerateEmbeddingsAsync(list, cancellationToken: cancellationToken).ConfigureAwait(false);
        return embeddings.Select(e => new Embedding(e)).ToArray();
    }
}
