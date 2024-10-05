// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.ML.OnnxRuntimeGenAI;
using static Microsoft.KernelMemory.OnnxConfig;

namespace Microsoft.KernelMemory.AI.Onnx;

/// <summary>
/// Text generator based on ONNX models, via OnnxRuntimeGenAi
/// See https://github.com/microsoft/onnxruntime-genai
/// </summary>
[Experimental("KMEXP01")]
public sealed class OnnxTextGenerator : ITextGenerator, IDisposable
{
    /// <summary>
    /// The ONNX Model used for text generation
    /// </summary>
    private readonly Model _model;

    /// <summary>
    /// Tokenizer used with the Onnx Generator and Model classes to produce tokens.
    /// This has the potential to contain a null value, depending on the contents of the Model Directory.
    /// </summary>
    private readonly Tokenizer? _tokenizer = default;

    /// <summary>
    /// Tokenizer used for GetTokens() and CountTokens()
    /// </summary>
    private readonly ITextTokenizer _textTokenizer;

    private readonly ILogger<OnnxTextGenerator> _log;

    private readonly OnnxConfig _config;

    /// <inheritdoc/>
    public int MaxTokenTotal { get; internal set; }

    /// <summary>
    /// Create a new instance
    /// </summary>
    /// <param name="config">Configuration settings</param>
    /// <param name="textTokenizer">Text Tokenizer</param>
    /// <param name="loggerFactory">Application Logger instance</param>
    public OnnxTextGenerator(
        OnnxConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
    {
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<OnnxTextGenerator>();
        if (textTokenizer == null)
        {
            this._log.LogWarning(
                "Tokenizer not specified, will use {0}. The token count might be incorrect, causing unexpected errors",
                nameof(GPT4oTokenizer));
            textTokenizer = new GPT4oTokenizer();
        }

        config.Validate();
        this._config = config;
        this.MaxTokenTotal = (int)config.MaxTokens;
        this._textTokenizer = textTokenizer;

        var modelDir = Path.GetFullPath(config.TextModelDir);
        var modelFile = Directory.GetFiles(modelDir)
            .FirstOrDefault(file => string.Equals(Path.GetExtension(file), ".ONNX", StringComparison.OrdinalIgnoreCase));

        this._log.LogDebug("Loading Onnx model: {1} from directory {0}", modelDir, Path.GetFileNameWithoutExtension(modelFile));
        this._model = new Model(config.TextModelDir);
        this._tokenizer = new Tokenizer(this._model);
        this._log.LogDebug("Onnx model loaded");
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> GenerateTextAsync(
        string prompt,
        TextGenerationOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var tokens = this._tokenizer?.Encode(prompt);
        using var generatorParams = new GeneratorParams(this._model);

        generatorParams.SetSearchOption("max_length", this.MaxTokenTotal);
        generatorParams.SetSearchOption("min_length", this._config.MinLength);
        generatorParams.SetSearchOption("num_return_sequences", this._config.ResultsPerPrompt);
        generatorParams.SetSearchOption("repetition_penalty", this._config.RepetitionPenalty);
        generatorParams.SetSearchOption("length_penalty", this._config.LengthPenalty);
        generatorParams.SetSearchOption("temperature", 0);

        if (options != null)
        {
            generatorParams.SetSearchOption("num_return_sequences", options.ResultsPerPrompt);
            generatorParams.SetSearchOption("temperature", options.Temperature);

            if (options.MaxTokens > 0)
            {
                generatorParams.SetSearchOption("max_length", (int)options.MaxTokens);
            }
        }

        switch (this._config.SearchType)
        {
            case OnnxSearchType.BeamSearch:
                generatorParams.SetSearchOption("do_sample", false);
                generatorParams.SetSearchOption("early_stopping", this._config.EarlyStopping);

                if (this._config.NumBeams != null)
                {
                    generatorParams.SetSearchOption("num_beams", (double)this._config.NumBeams);
                }

                break;

            case OnnxSearchType.TopN:
                generatorParams.SetSearchOption("do_sample", true);
                generatorParams.SetSearchOption("top_k", this._config.TopK);

                generatorParams.SetSearchOption("top_p", options is { NucleusSampling: > 0 and <= 1 }
                    ? options.NucleusSampling
                    : this._config.NucleusSampling);

                break;

            default:

                generatorParams.SetSearchOption("do_sample", false);

                if (this._config.NumBeams != null)
                {
                    generatorParams.SetSearchOption("num_beams", (double)this._config.NumBeams);
                }

                break;
        }

        generatorParams.SetInputSequences(tokens);

        using (var generator = new Generator(this._model, generatorParams))
        {
            List<int> outputTokens = new();

            while (!generator.IsDone() && cancellationToken.IsCancellationRequested == false)
            {
                generator.ComputeLogits();
                generator.GenerateNextToken();

                outputTokens.AddRange(generator.GetSequence(0));

                if (outputTokens.Count > 0 && this._tokenizer != null)
                {
                    var newToken = outputTokens[^1];
                    yield return this._tokenizer.Decode(new int[] { newToken });
                }
            }
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public int CountTokens(string text)
    {
        // TODO: Implement with _tokenizer and remove _textTokenizer
        return this._textTokenizer.CountTokens(text);
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetTokens(string text)
    {
        // TODO: Implement with _tokenizer and remove _textTokenizer
        return this._textTokenizer.GetTokens(text);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this._model?.Dispose();
        this._tokenizer?.Dispose();
    }
}
