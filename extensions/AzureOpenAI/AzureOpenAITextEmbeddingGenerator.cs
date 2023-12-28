﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Microsoft.KernelMemory.AI.AzureOpenAI;

public class AzureOpenAITextEmbeddingGenerator : ITextEmbeddingGenerator
{
    private readonly ITextTokenizer _textTokenizer;
    private readonly ILogger<AzureOpenAITextEmbeddingGenerator> _log;
    private readonly AzureOpenAITextEmbeddingGenerationService _client;

    public AzureOpenAITextEmbeddingGenerator(
        AzureOpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
        : this(config, textTokenizer, loggerFactory?.CreateLogger<AzureOpenAITextEmbeddingGenerator>())
    {
    }

    public AzureOpenAITextEmbeddingGenerator(
        AzureOpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        ILogger<AzureOpenAITextEmbeddingGenerator>? log = null)
    {
        this._log = log ?? DefaultLogger<AzureOpenAITextEmbeddingGenerator>.Instance;

        if (textTokenizer == null)
        {
            this._log.LogWarning(
                "Tokenizer not specified, will use {0}. The token count might be incorrect, causing unexpected errors",
                nameof(DefaultGPTTokenizer));
            textTokenizer = new DefaultGPTTokenizer();
        }

        this._textTokenizer = textTokenizer;

        this.MaxTokens = config.MaxTokenTotal;

        switch (config.Auth)
        {
            case AzureOpenAIConfig.AuthTypes.AzureIdentity:
                this._client = new AzureOpenAITextEmbeddingGenerationService(
                    deploymentName: config.Deployment,
                    modelId: config.Deployment,
                    endpoint: config.Endpoint,
                    credential: new DefaultAzureCredential());
                break;

            case AzureOpenAIConfig.AuthTypes.APIKey:
                this._client = new AzureOpenAITextEmbeddingGenerationService(
                    deploymentName: config.Deployment,
                    modelId: config.Deployment,
                    endpoint: config.Endpoint,
                    apiKey: config.APIKey);
                break;

            default:
                throw new NotImplementedException($"Azure OpenAI auth type '{config.Auth}' not available");
        }
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
