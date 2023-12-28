// Copyright (c) Microsoft. All rights reserved.

using FunctionalTests.TestHelpers;
using Microsoft.KernelMemory;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Xunit.Abstractions;

namespace FunctionalTests.ServerLess;

public class OpenAIDependencyInjectionTest : BaseTestCase
{
    private readonly string _openAIKey;

    public OpenAIDependencyInjectionTest(
        IConfiguration cfg,
        ITestOutputHelper output) : base(cfg, output)
    {
        this._openAIKey = this.OpenAIConfiguration.GetValue<string>("APIKey")
                          ?? throw new TestCanceledException("OpenAI API key is missing");
    }

    [Fact]
    [Trait("Category", "Serverless")]
    public async Task TestExtensionMethod1()
    {
        // Arrange
        var memory = new KernelMemoryBuilder()
            .WithOpenAIDefaults(this._openAIKey)
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
            .WithOpenAI(new OpenAIConfig
            {
                APIKey = this._openAIKey,
                EmbeddingModel = "text-embedding-ada-002",
                TextModel = "gpt-3.5-turbo-16k"
            })
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
                APIKey = this._openAIKey,
                EmbeddingModel = "text-embedding-ada-002",
            })
            .WithOpenAITextGeneration(new OpenAIConfig
            {
                APIKey = this._openAIKey,
                TextModel = "gpt-3.5-turbo-16k"
            })
            .Build<MemoryServerless>();

        // Act
        await memory.ImportTextAsync("Today is November 1st, 2099");

        // Assert
        var answer = await memory.AskAsync("What year is it?");
        Assert.Contains("2099", answer.Result);
    }
}
