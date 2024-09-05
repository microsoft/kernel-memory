// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Diagnostics;
using OllamaSharp;
using OllamaSharp.Models;

namespace Microsoft.KernelMemory.AI.Ollama;

public class OllamaTextGenerator : ITextGenerator
{
    private const int MaxTokensIfUndefined = 4096;

    private readonly IOllamaApiClient _client;
    private readonly OllamaModelConfig _modelConfig;
    private readonly ILogger<OllamaTextGenerator> _log;
    private readonly ITextTokenizer _textTokenizer;

    public int MaxTokenTotal { get; }

    public OllamaTextGenerator(
        IOllamaApiClient ollamaClient,
        OllamaModelConfig modelConfig,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
    {
        this._client = ollamaClient;
        this._modelConfig = modelConfig;
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<OllamaTextGenerator>();

        if (textTokenizer == null)
        {
            this._log.LogWarning(
                "Tokenizer not specified, will use {0}. The token count might be incorrect, causing unexpected errors",
                nameof(GPT4oTokenizer));
            textTokenizer = new GPT4oTokenizer();
        }

        this._textTokenizer = textTokenizer;

        this.MaxTokenTotal = modelConfig.MaxTokenTotal ?? MaxTokensIfUndefined;
    }

    public OllamaTextGenerator(
        OllamaConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
        : this(
            new OllamaApiClient(new Uri(config.Endpoint), config.TextModel.ModelName),
            config.TextModel,
            textTokenizer,
            loggerFactory)
    {
    }

    public OllamaTextGenerator(
        HttpClient httpClient,
        OllamaConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
        : this(
            new OllamaApiClient(httpClient, config.TextModel.ModelName),
            config.TextModel,
            textTokenizer,
            loggerFactory)
    {
    }

    public int CountTokens(string text)
    {
        return this._textTokenizer.CountTokens(text);
    }

    public IReadOnlyList<string> GetTokens(string text)
    {
        return this._textTokenizer.GetTokens(text);
    }

    public async IAsyncEnumerable<string> GenerateTextAsync(
        string prompt,
        TextGenerationOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new GenerateRequest
        {
            Model = this._client.SelectedModel,
            Prompt = prompt,
            Stream = true,
            Options = new RequestOptions
            {
                // Use case specific
                Temperature = (float)options.Temperature,
                TopP = (float)options.NucleusSampling,
                RepeatPenalty = (float)options.FrequencyPenalty,

                // Global settings
                MiroStat = this._modelConfig.MiroStat,
                MiroStatEta = this._modelConfig.MiroStatEta,
                MiroStatTau = this._modelConfig.MiroStatTau,
                NumCtx = this._modelConfig.NumCtx,
                NumGqa = this._modelConfig.NumGqa,
                NumGpu = this._modelConfig.NumGpu,
                NumThread = this._modelConfig.NumThread,
                RepeatLastN = this._modelConfig.RepeatLastN,
                Seed = this._modelConfig.Seed,
                TfsZ = this._modelConfig.TfsZ,
                NumPredict = this._modelConfig.NumPredict,
                TopK = this._modelConfig.TopK,
                MinP = this._modelConfig.MinP,
            }
        };

        if (options.StopSequences is { Count: > 0 })
        {
            var stop = new List<string>();
            foreach (var s in options.StopSequences) { stop.Add(s); }

            request.Options.Stop = stop.ToArray();
        }

        // IAsyncEnumerable<GenerateResponseStream?> stream = this._client.Generate(request, cancellationToken);
        // await foreach (GenerateResponseStream? token in stream)
        // {
        //     if (token != null) { yield return token.Response; }
        // }

        var chat = new Chat(this._client);
        IAsyncEnumerable<string?> stream = chat.Send(prompt, cancellationToken);
        await foreach (string? token in stream)
        {
            if (token != null) { yield return token; }
        }
    }
}
