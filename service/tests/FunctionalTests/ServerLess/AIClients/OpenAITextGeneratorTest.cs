// Copyright (c) Microsoft. All rights reserved.

using FunctionalTests.TestHelpers;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Diagnostics;
using Xunit.Abstractions;

namespace FunctionalTests.ServerLess.AIClients;

public sealed class OpenAITextGeneratorTest : BaseTestCase
{
    private readonly OpenAIConfig _config;

    public OpenAITextGeneratorTest(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
        this._config = new OpenAIConfig
        {
            APIKey = this.Configuration.GetSection("Services").GetSection("OpenAI").GetValue<string>("APIKey") ?? "",
            OrgId = this.Configuration.GetSection("Services").GetSection("OpenAI").GetValue<string>("OrgId") ?? "",
            MaxRetries = 1
        };
    }

    [Fact]
    public async Task ItStreamsFromChatModel()
    {
        // Arrange
        this._config.TextModel = "gpt-4";
        var client = new OpenAITextGenerator(this._config, null, DefaultLogger<OpenAITextGenerator>.Instance);

        // Act
        IAsyncEnumerable<string> text = client.GenerateTextAsync(
            "write 100 words about the Earth", new TextGenerationOptions());

        // Assert
        await foreach (string word in text)
        {
            Console.Write(word);
        }
    }

    [Fact]
    public async Task ItStreamsFromTextModel()
    {
        // Arrange
        this._config.TextModel = "text-davinci-003";
        var client = new OpenAITextGenerator(this._config, null, DefaultLogger<OpenAITextGenerator>.Instance);

        // Act
        IAsyncEnumerable<string> text = client.GenerateTextAsync(
            "write 100 words about the Earth", new TextGenerationOptions());

        // Assert
        await foreach (string word in text)
        {
            Console.Write(word);
        }
    }
}
