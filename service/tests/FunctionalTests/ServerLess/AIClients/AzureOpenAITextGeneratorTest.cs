// Copyright (c) Microsoft. All rights reserved.

using FunctionalTests.TestHelpers;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.AzureOpenAI;
using Xunit.Abstractions;

namespace FunctionalTests.ServerLess.AIClients;

public class AzureOpenAITextGeneratorTest : BaseTestCase
{
    private readonly AzureOpenAIConfig _config;

    public AzureOpenAITextGeneratorTest(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
        var config = this.Configuration.GetSection("Services").GetSection("AzureOpenAIText");
        this._config = new AzureOpenAIConfig
        {
            Auth = AzureOpenAIConfig.AuthTypes.APIKey,
            Endpoint = config.GetValue<string>("Endpoint") ?? "",
            Deployment = config.GetValue<string>("Deployment") ?? "",
            APIKey = config.GetValue<string>("APIKey") ?? "",
        };
    }

    [Fact]
    public async Task ItStreamsFromChatModel()
    {
        // Arrange
        this._config.APIType = AzureOpenAIConfig.APITypes.ChatCompletion;
        var client = new AzureOpenAITextGenerator(this._config, loggerFactory: null);

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
