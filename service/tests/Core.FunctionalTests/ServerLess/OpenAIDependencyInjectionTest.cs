// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.TestHelpers;
using Xunit.Abstractions;

namespace FunctionalTests.ServerLess;

public class OpenAIDependencyInjectionTest : BaseFunctionalTestCase
{
    public OpenAIDependencyInjectionTest(
        IConfiguration cfg,
        ITestOutputHelper output) : base(cfg, output)
    {
    }

    [Fact]
    [Trait("Category", "Serverless")]
    public async Task TestExtensionMethod1()
    {
        // Arrange
        var memory = new KernelMemoryBuilder()
            .WithOpenAIDefaults(this.OpenAiConfig.APIKey)
            .Build<MemoryServerless>();

        // Act
        await memory.ImportTextAsync("Today is November 1st, 2099");

        // Assert
        var answer = await memory.AskAsync("What year is it?");
        Assert.Contains("2099", answer.Result);
    }

    [Fact]
    [Trait("Category", "Serverless")]
    public async Task TestExtensionMethod2()
    {
        // Arrange
        var memory = new KernelMemoryBuilder()
            .WithOpenAI(this.OpenAiConfig)
            .Build<MemoryServerless>();

        // Act
        await memory.ImportTextAsync("Today is November 1st, 2099");

        // Assert
        var answer = await memory.AskAsync("What year is it?");
        Assert.Contains("2099", answer.Result);
    }

    [Fact]
    [Trait("Category", "Serverless")]
    public async Task TestExtensionMethod3()
    {
        // Arrange
        var memory = new KernelMemoryBuilder()
            .WithOpenAITextEmbeddingGeneration(new OpenAIConfig
            {
                APIKey = this.OpenAiConfig.APIKey,
                EmbeddingModel = "text-embedding-ada-002",
            })
            .WithOpenAITextGeneration(new OpenAIConfig
            {
                APIKey = this.OpenAiConfig.APIKey,
                TextModel = "gpt-4"
            })
            .Build<MemoryServerless>();

        // Act
        await memory.ImportTextAsync("Today is November 1st, 2099");

        // Assert
        var answer = await memory.AskAsync("What year is it?");
        Console.WriteLine("answer: " + answer.Result);
        Assert.Contains("2099", answer.Result, StringComparison.OrdinalIgnoreCase);
    }
}
