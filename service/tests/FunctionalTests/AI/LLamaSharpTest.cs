// Copyright (c) Microsoft. All rights reserved.

using FunctionalTests.TestHelpers;
using Microsoft.KernelMemory.AI.Llama;
using Xunit.Abstractions;

namespace FunctionalTests.AI;

public class LLamaSharpTest : BaseTestCase
{
    private readonly LlamaSharpTextGenerator _textGenerator;

    public LLamaSharpTest(
        IConfiguration cfg,
        ITestOutputHelper output) : base(cfg, output)
    {
        var config = new LlamaSharpConfig();
        this.Configuration.BindSection("Services:LlamaSharp", config);
        config.Validate();
        this._textGenerator = new LlamaSharpTextGenerator(config, loggerFactory: null);
    }

    [Fact]
    public void ItCountsTokens()
    {
        // Act
        var tokenCount = this._textGenerator.CountTokens("hello world");

        // Assert
        Console.WriteLine("Token count: " + tokenCount);
        Assert.True(tokenCount > 0);
    }
}
