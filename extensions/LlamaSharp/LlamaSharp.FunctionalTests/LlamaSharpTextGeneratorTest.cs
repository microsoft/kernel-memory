// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics;
using System.Text;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.LlamaSharp;
using Microsoft.KernelMemory.AI.OpenAI.GPT3;
using Microsoft.TestHelpers;
using Xunit.Abstractions;

namespace LlamaSharp.FunctionalTests;

public sealed class LlamaSharpTextGeneratorTest : BaseFunctionalTestCase
{
    private readonly LlamaSharpTextGenerator _target;
    private readonly Stopwatch _timer;

    public LlamaSharpTextGeneratorTest(
        IConfiguration cfg,
        ITestOutputHelper output) : base(cfg, output)
    {
        this._timer = new Stopwatch();

        this.LlamaSharpConfig.Validate();
        this._target = new LlamaSharpTextGenerator(this.LlamaSharpConfig, loggerFactory: null);
        var modelFilename = this.LlamaSharpConfig.ModelPath.Split('/').Last().Split('\\').Last();
        Console.WriteLine($"Model in use: {modelFilename}");
    }

    [Fact]
    [Trait("Category", "LlamaSharp")]
    public void ItCountsTokens()
    {
        // Arrange
        var text = "hello world, we can run llama";

        // Act
        this._timer.Restart();
        var tokenCount = this._target.CountTokens(text);
        this._timer.Stop();

        // Assert
        Console.WriteLine("Llama token count: " + tokenCount);
        Console.WriteLine("GPT3 token count: " + GPT3Tokenizer.Encode(text).Count);
        Console.WriteLine($"Time: {this._timer.ElapsedMilliseconds / 1000} secs");

        // Note: value for llama-2-13b.Q2_K.gguf
        Assert.Equal(8, tokenCount);
    }

    [Fact]
    [Trait("Category", "LlamaSharp")]
    public void ItCountsTokensOfEmptyStrings()
    {
        // Act - No Exceptions should occur
        this._target.CountTokens("");
        this._target.CountTokens("\r");

        // To be fixed by LLamaSharp
        // See https://github.com/SciSharp/LLamaSharp/issues/430
        // this._target.CountTokens("\n");
        // this._target.CountTokens("\n\n");
        // this._target.CountTokens("\t");
        // this._target.CountTokens("\t\t");
        // this._target.CountTokens("\v");
        // this._target.CountTokens("\v\v");
        // this._target.CountTokens("\0");
        // this._target.CountTokens("\0\0");
        // this._target.CountTokens("\b");
        // this._target.CountTokens("\b\b");
    }

    [Fact]
    [Trait("Category", "LlamaSharp")]
    public async Task ItGeneratesText()
    {
        // Arrange
        var prompt = """
                     # Current date: 12/12/2024.
                     # Instructions: use JSON syntax.
                     # Deduction: { "DayOfWeek": "Monday", "MonthName":
                     """;
        var options = new TextGenerationOptions
        {
            MaxTokens = 60,
            Temperature = 0,
            StopSequences = new List<string> { "Question" }
        };

        // Act
        this._timer.Restart();
        var tokens = this._target.GenerateTextAsync(prompt, options);
        var result = new StringBuilder();
        await foreach (string token in tokens)
        {
            // Console.WriteLine(token);
            result.Append(token);
        }

        this._timer.Stop();
        var answer = result.ToString();

        // Assert
        Console.WriteLine($"Model Output:\n=============================\n{answer}\n=============================");
        Console.WriteLine($"Time: {this._timer.ElapsedMilliseconds / 1000} secs");
        Assert.Contains("december", answer, StringComparison.OrdinalIgnoreCase);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        this._target.Dispose();
    }
}
