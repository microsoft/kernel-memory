// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.Tokenizers;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;

namespace Microsoft.KernelMemory.AI.OpenAI;

public class OpenAITextEmbeddingGenerator : ITextEmbeddingGenerator
{
    private readonly ITextTokenizer _textTokenizer;
    private readonly OpenAITextEmbeddingGeneration _client;

    public OpenAITextEmbeddingGenerator(
        OpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
        : this(config, textTokenizer, loggerFactory?.CreateLogger<OpenAITextEmbeddingGenerator>())
    {
    }

    public OpenAITextEmbeddingGenerator(
        OpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        ILogger<OpenAITextEmbeddingGenerator>? log = null)
    {
        this.MaxTokens = config.EmbeddingModelMaxTokenTotal;
        this._textTokenizer = textTokenizer ?? new DefaultGPTTokenizer();

        this._client = new OpenAITextEmbeddingGeneration(
            modelId: config.EmbeddingModel,
            apiKey: config.APIKey,
            organization: config.OrgId);
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
