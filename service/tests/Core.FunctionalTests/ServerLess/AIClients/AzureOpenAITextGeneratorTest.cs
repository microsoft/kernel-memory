// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.AzureOpenAI;
using Microsoft.KM.TestHelpers;
using Xunit.Abstractions;

namespace Microsoft.KM.Core.FunctionalTests.ServerLess.AIClients;

public class AzureOpenAITextGeneratorTest : BaseFunctionalTestCase
{
    private readonly AzureOpenAIConfig _config;

    public AzureOpenAITextGeneratorTest(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
        this._config = this.AzureOpenAITextConfiguration;
    }

    [Fact]
    [Trait("Category", "Serverless")]
    public async Task ItStreamsFromChatModel()
    {
        // Arrange
        this._config.APIType = AzureOpenAIConfig.APITypes.ChatCompletion;
        var client = new AzureOpenAITextGenerator(this._config, loggerFactory: null);

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
