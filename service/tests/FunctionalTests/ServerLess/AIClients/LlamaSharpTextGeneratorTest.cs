// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics;
using System.Text;
using FunctionalTests.TestHelpers;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.Llama;
using Microsoft.KernelMemory.AI.Tokenizers.GPT3;
using Xunit.Abstractions;

namespace FunctionalTests.AI;

public sealed class LlamaSharpTextGeneratorTest : BaseTestCase
{
    private readonly LlamaSharpTextGenerator _target;
    private readonly Stopwatch _timer;

    public LlamaSharpTextGeneratorTest(
        IConfiguration cfg,
        ITestOutputHelper output) : base(cfg, output)
    {
        var config = new LlamaSharpConfig();
        this.Configuration.BindSection("Services:LlamaSharp", config);
        this._target = new LlamaSharpTextGenerator(config, loggerFactory: null);
        this._timer = new Stopwatch();
        var modelFilename = config.ModelPath.Split('/').Last().Split('\\').Last();
        Console.WriteLine($"Model in use: {modelFilename}");
    }

    [Fact]
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

        Assert.Equal(8, tokenCount);
    }

    [Fact]
    public async Task ItGeneratesText()
    {
        // Arrange
        var prompt = """
                     Facts:
                     The public Kernel Memory project kicked off around May 2023.
                     Now, in December 2023, we are integrating LLama compatibility
                     into KM, following the steady addition of numerous features.
                     By January, we anticipate to complete this update and potentially
                     introduce more models by February.
                     Instructions: Reply in JSON.
                     Question: What's the current month?
                     """;
        var options = new TextGenerationOptions
        {
            MaxTokens = 30,
            Temperature = 0,
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
        Console.WriteLine($"=============================\n{answer}\n=============================");
        Console.WriteLine($"Time: {this._timer.ElapsedMilliseconds / 1000} secs");
        Assert.Contains("december", answer, StringComparison.OrdinalIgnoreCase);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        this._target.Dispose();
    }
}
