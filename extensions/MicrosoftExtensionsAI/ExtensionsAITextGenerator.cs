// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;

namespace Microsoft.KernelMemory.AI.ExtensionsAI;

/// <summary>Provides an <see cref="ITextGenerator" /> that wraps an <see cref="IChatClient"/>.</summary>
public sealed class ExtensionsAITextGenerator : ITextGenerator
{
    private readonly IChatClient _client;
    private readonly ITextTokenizer _textTokenizer;
    private readonly ILogger<ExtensionsAITextGenerator> _log;
    private readonly string _textModel;

    /// <summary>Initializes a new instance of the <see cref="ExtensionsAITextGenerator"/> class.</summary>
    /// <param name="chatClient">The underlying <see cref="IChatClient"/>.</param>
    /// <param name="config">Optional configuration for the instance.</param>
    /// <param name="textTokenizer">Optional text tokenizer to use for token counting.</param>
    /// <param name="loggerFactory">Optional logging factory to use for logging.</param>
    public ExtensionsAITextGenerator(
        IChatClient chatClient,
        ExtensionsAIConfig? config = null,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullExceptionEx.ThrowIfNull(chatClient);

        config ??= new();

        this._client = chatClient;
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<ExtensionsAITextGenerator>();
        this._textModel = this._client.GetService<ChatClientMetadata>()?.DefaultModelId ?? "";
        this.MaxTokenTotal = config.MaxTokens;
        this._textTokenizer = textTokenizer ?? TokenizerFactory.GetTokenizerForEncoding(string.IsNullOrEmpty(config.Tokenizer) ? "o200k" : config.Tokenizer)!;
    }

    /// <inheritdoc/>
    public int MaxTokenTotal { get; }

    /// <inheritdoc/>
    public int CountTokens(string text) => this._textTokenizer.CountTokens(text);

    /// <inheritdoc/>
    public IReadOnlyList<string> GetTokens(string text) => this._textTokenizer.GetTokens(text);

    /// <inheritdoc/>
    public async IAsyncEnumerable<GeneratedTextContent> GenerateTextAsync(
        string prompt,
        TextGenerationOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ChatOptions chatOptions = new()
        {
            FrequencyPenalty = (float)options.FrequencyPenalty,
            MaxOutputTokens = options.MaxTokens ?? this.MaxTokenTotal,
            PresencePenalty = (float)options.PresencePenalty,
            StopSequences = options.StopSequences,
            Temperature = (float)options.Temperature,
            TopP = (float)options.NucleusSampling,
        };

        string? responseModel = null;
        await foreach (var update in this._client.GetStreamingResponseAsync(prompt, chatOptions, cancellationToken).WithCancellation(cancellationToken))
        {
            responseModel ??= update.ModelId;
            foreach (var content in update.Contents)
            {
                switch (content)
                {
                    case Microsoft.Extensions.AI.TextContent tc:
                        yield return new GeneratedTextContent(tc.Text, null);
                        break;

                    case UsageContent uc:
                        yield return new GeneratedTextContent(string.Empty, new TokenUsage
                        {
                            Timestamp = update.CreatedAt ?? DateTimeOffset.UtcNow,
                            ModelType = Constants.ModelType.TextGeneration,
                            ModelName = responseModel ?? this._textModel,
                            ServiceTokensIn = (int?)uc.Details.InputTokenCount,
                            ServiceTokensOut = (int?)uc.Details.OutputTokenCount,
                        });
                        break;
                }
            }
        }
    }
}
