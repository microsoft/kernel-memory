// Copyright (c) Microsoft. All rights reserved.

using FunctionalTests.TestHelpers;
using Microsoft.KernelMemory;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Xunit.Abstractions;

namespace FunctionalTests.ServerLess;

// ReSharper disable InconsistentNaming
public class OpenAITests : BaseTestCase
{
    private readonly string _openAIKey;

    public OpenAITests(
        IConfiguration cfg,
        ITestOutputHelper output) : base(cfg, output)
    {
        this._openAIKey = this.OpenAIConfiguration.GetValue<string>("APIKey")
                          ?? throw new TestCanceledException("OpenAI API key is missing");
    }

    [Fact]
    public async Task ItTest1()
    {
        // Arrange
        var memory = new KernelMemoryBuilder()
            .WithOpenAIDefaults(this._openAIKey)
            .BuildServerlessClient();

        // Act
        await memory.ImportTextAsync("Today is November 1 2099");

        // Assert
        var answer = await memory.AskAsync("What's year is it?");
        Assert.Contains("2099", answer.Result);
    }

    [Fact]
    public async Task ItTest2()
    {
        // Arrange
        var memory = new KernelMemoryBuilder()
            .WithOpenAI(new OpenAIConfig
            {
                APIKey = this._openAIKey,
                EmbeddingModel = "text-embedding-ada-002",
                TextModel = "gpt-3.5-turbo-16k"
            })
            .BuildServerlessClient();

        // Act
        await memory.ImportTextAsync("Today is November 1 2099");

        // Assert
        var answer = await memory.AskAsync("What's year is it?");
        Assert.Contains("2099", answer.Result);
    }

    [Fact]
    public async Task ItTest3()
    {
        // Arrange
        var memory = new KernelMemoryBuilder()
            .WithOpenAITextEmbedding(new OpenAIConfig
            {
                APIKey = this._openAIKey,
                EmbeddingModel = "text-embedding-ada-002",
            })
            .WithOpenAITextGeneration(new OpenAIConfig
            {
                APIKey = this._openAIKey,
                TextModel = "gpt-3.5-turbo-16k"
            })
            .BuildServerlessClient();

        // Act
        await memory.ImportTextAsync("Today is November 1 2099");

        // Assert
        var answer = await memory.AskAsync("What's year is it?");
        Assert.Contains("2099", answer.Result);
    }
}
