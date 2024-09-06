// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Diagnostics;
using OpenAI;
using OpenAI.Embeddings;

namespace Microsoft.KernelMemory.AI.AzureOpenAI;

[Experimental("KMEXP01")]
public sealed class AzureOpenAITextEmbeddingGenerator : ITextEmbeddingGenerator, ITextEmbeddingBatchGenerator
{
    private readonly ITextTokenizer _textTokenizer;
    private readonly OpenAIClient _client;
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
                nameof(GPT4oTokenizer));
            textTokenizer = new GPT4oTokenizer();
        }

        this._textTokenizer = textTokenizer;
        this._deployment = config.Deployment;

        this.MaxTokens = config.MaxTokenTotal;
        this.MaxBatchSize = config.MaxEmbeddingBatchSize;

        switch (config.Auth)
        {
            case AzureOpenAIConfig.AuthTypes.AzureIdentity:
                this._client = new AzureOpenAIClient(new Uri(config.Endpoint), new DefaultAzureCredential());
                break;

            case AzureOpenAIConfig.AuthTypes.ManualTokenCredential:
                this._client = new AzureOpenAIClient(new Uri(config.Endpoint), config.GetTokenCredential());
                break;

            case AzureOpenAIConfig.AuthTypes.APIKey:
                if (string.IsNullOrEmpty(config.APIKey))
                {
                    throw new ConfigurationException($"Azure OpenAI: {config.APIKey} is empty");
                }

                this._client = new AzureOpenAIClient(new Uri(config.Endpoint), new AzureKeyCredential(config.APIKey));
                break;

            default:
                throw new ConfigurationException($"Azure OpenAI: authentication type '{config.Auth:G}' is not supported");
                // case AzureOpenAIConfig.AuthTypes.AzureIdentity:
                //     this._client = new AzureOpenAITextEmbeddingGenerationService(
                //         deploymentName: config.Deployment,
                //         endpoint: config.Endpoint,
                //         credential: new DefaultAzureCredential(),
                //         modelId: config.Deployment,
                //         httpClient: httpClient,
                //         dimensions: config.EmbeddingDimensions,
                //         loggerFactory: loggerFactory);
                //     break;

                // case AzureOpenAIConfig.AuthTypes.ManualTokenCredential:
                //     this._client = new AzureOpenAITextEmbeddingGenerationService(
                //         deploymentName: config.Deployment,
                //         endpoint: config.Endpoint,
                //         credential: config.GetTokenCredential(),
                //         modelId: config.Deployment,
                //         httpClient: httpClient,
                //         dimensions: config.EmbeddingDimensions,
                //         loggerFactory: loggerFactory);
                //     break;

                // case AzureOpenAIConfig.AuthTypes.APIKey:
                //     this._client = new AzureOpenAITextEmbeddingGenerationService(
                //         deploymentName: config.Deployment,
                //         endpoint: config.Endpoint,
                //         apiKey: config.APIKey,
                //         modelId: config.Deployment,
                //         httpClient: httpClient,
                //         dimensions: config.EmbeddingDimensions,
                //         loggerFactory: loggerFactory);
                //     break;

                // default:
                //     throw new NotImplementedException($"Azure OpenAI auth type '{config.Auth}' not available");
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
    public async Task<Embedding> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        this._log.LogTrace("Generating embedding, deployment '{0}'", this._deployment);
        var embeddingClient = this._client.GetEmbeddingClient(this._deployment);
        var embeddingGenerationOptions = new EmbeddingGenerationOptions();
        var openAIEmbedding = await embeddingClient.GenerateEmbeddingAsync(text, embeddingGenerationOptions, cancellationToken).ConfigureAwait(false);
        var kernelMemoryEmbedding = new Microsoft.KernelMemory.Embedding(openAIEmbedding.Value.Vector.ToArray());
        return kernelMemoryEmbedding;
    }

    /// <inheritdoc/>
    public async Task<Embedding[]> GenerateEmbeddingBatchAsync(IEnumerable<string> textList, CancellationToken cancellationToken = default)
    {
        var list = textList.ToList();
        this._log.LogTrace("Generating embeddings, deployment '{0}', batch size '{1}'", this._deployment, list.Count);
        var embeddingClient = this._client.GetEmbeddingClient(this._deployment);
        var embeddingGenerationOptions = new EmbeddingGenerationOptions();
        var openAIEmbeddings = await embeddingClient.GenerateEmbeddingsAsync(list, embeddingGenerationOptions, cancellationToken).ConfigureAwait(false);
        return openAIEmbeddings.Value.Select(e => new Embedding(e.Vector.ToArray())).ToArray();
    }
}
