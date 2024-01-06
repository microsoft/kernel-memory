// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.TestHelpers;
using Xunit.Abstractions;

namespace FunctionalTests.ServerLess.AIClients;

public sealed class OpenAITextGeneratorTest : BaseFunctionalTestCase
{
    private readonly OpenAIConfig _config;

    public OpenAITextGeneratorTest(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
        this._config = this.OpenAiConfig;
    }

    [Fact]
    [Trait("Category", "Serverless")]
    public async Task ItStreamsFromChatModel()
    {
        // Arrange
        this._config.TextModel = "gpt-4";
        var client = new OpenAITextGenerator(this._config, null, DefaultLogger<OpenAITextGenerator>.Instance);

        // Act
        IAsyncEnumerable<string> text = client.GenerateTextAsync(
            "write 100 words about the Earth", new TextGenerationOptions());

        // Assert
        var count = 0;
        await foreach (string word in text)
        {
            Console.Write(word);
            if (count++ > 10) { break; }
        }

        Assert.True(count > 10);
    }

    [Fact]
    [Trait("Category", "Serverless")]
    public async Task ItStreamsFromTextModel()
    {
        // Arrange
        this._config.TextModel = "text-davinci-003";
        var client = new OpenAITextGenerator(this._config, null, DefaultLogger<OpenAITextGenerator>.Instance);

        // Act
        IAsyncEnumerable<string> text = client.GenerateTextAsync(
            "write 100 words about the Earth", new TextGenerationOptions());

        // Assert
        var count = 0;
        await foreach (string word in text)
        {
            Console.Write(word);
            if (count++ > 10) { break; }
        }

        Assert.True(count > 10);
    }
}
