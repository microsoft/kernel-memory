// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.ML.OnnxRuntimeGenAI;

namespace Microsoft.KernelMemory.AI.Onnx;

/// <summary>
/// Text generator based on Onnx models, via OnnxRuntimeGenAi
/// See https://github.com/microsoft/onnxruntime-genai
/// </summary>
[Experimental("KMEXP01")]
public sealed class OnnxTextGenerator : ITextGenerator, IDisposable
{
    private readonly Model? _model = default;
    private readonly Tokenizer? _tokenizer = default;
    private readonly ILogger<OnnxTextGenerator> _log;
    private readonly ITextTokenizer _textTokenizer;

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
        this.MaxTokenTotal = (int)config.MaxLength;
        this.MinLength = config.MinLength;
        this.PastPresentShareBuffer = config.PastPresentShareBuffer;
        this._model = new Model(config.ModelPath);
        this._tokenizer = new Tokenizer(this._model);
        this._textTokenizer = textTokenizer;

        var modelFilename = config.ModelPath.TrimEnd('/').Split('/').Last();
        this._log.LogDebug("Loading Onnx model: {0}", modelFilename);
        this._model = new Model(config.ModelPath);
        this._tokenizer = new Tokenizer(this._model);
        this._log.LogDebug("Onnx model loaded");
    }

    /// <inheritdoc/>
    public int MaxTokenTotal { get; }

    /// <inheritdoc/>
    public double MinLength { get; }

    /// <inheritdoc/>
    public bool PastPresentShareBuffer { get; }

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> GenerateTextAsync(
        string prompt,
        TextGenerationOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var tokens = this._tokenizer?.Encode(prompt);
        using var generatorParams = new GeneratorParams(this._model);

        generatorParams.SetSearchOption("max_length", (double)this.MaxTokenTotal);
        generatorParams.SetSearchOption("min_length", this.MinLength);
        generatorParams.SetSearchOption("past_present_share_buffer", this.PastPresentShareBuffer);

        if (options != null)
        {
            generatorParams.SetSearchOption("temperature", options.Temperature);
        }

        await Task.Run(() => generatorParams.SetInputSequences(tokens), cancellationToken).ConfigureAwait(true);

        using (var generator = new Generator(this._model, generatorParams))
        {
            List<int> outputTokens = new();

            while (!generator.IsDone() && cancellationToken.IsCancellationRequested == false)
            {
                generator.ComputeLogits();
                generator.GenerateNextToken();

                if (outputTokens.Count > 0 && this._tokenizer != null)
                {
                    var newToken = outputTokens[^1];
                    yield return this._tokenizer.Decode(new int[] { newToken });
                }
            }
        }
    }

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
    public void Dispose()
    {
        this.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    private void Dispose(bool disposing)
    {
        if (!disposing) { return; }

        this._model?.Dispose();
        this._tokenizer?.Dispose();
    }

    ~OnnxTextGenerator()
    {
        this.Dispose(false);
    }
}
