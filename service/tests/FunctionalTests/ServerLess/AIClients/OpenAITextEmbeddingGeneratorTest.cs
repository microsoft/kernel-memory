// Copyright (c) Microsoft. All rights reserved.

using FunctionalTests.TestHelpers;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.OpenAI;
using Xunit.Abstractions;

namespace FunctionalTests.ServerLess.AIClients;

// ReSharper disable StringLiteralTypo
public sealed class OpenAITextEmbeddingGeneratorTest : BaseTestCase
{
    private readonly OpenAITextEmbeddingGenerator _target;

    public OpenAITextEmbeddingGeneratorTest(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
        var config = new OpenAIConfig
        {
            APIKey = this.Configuration.GetSection("Services").GetSection("OpenAI").GetValue<string>("APIKey") ?? "",
            OrgId = this.Configuration.GetSection("Services").GetSection("OpenAI").GetValue<string>("OrgId") ?? "",
            EmbeddingModel = "text-embedding-ada-002",
            MaxRetries = 3,
        };

        this._target = new OpenAITextEmbeddingGenerator(config, loggerFactory: null);
    }

    [Fact]
    public async Task ItGeneratesEmbeddingsThatCaptureSemantics()
    {
        // Arrange
        const string Text1 = "It's January 12th, sunny but quite cold outside";
        const string Text2 = "E' il 12 gennaio, c'e' il sole ma fa freddo fuori";
        const string Text3 = "the cat is white";

        // Act
        var e1 = await this._target.GenerateEmbeddingAsync(Text1);
        var e2 = await this._target.GenerateEmbeddingAsync(Text2);
        var e3 = await this._target.GenerateEmbeddingAsync(Text3);

        // Assert
        Console.WriteLine("e1 <--> e2: " + e1.CosineSimilarity(e2));
        Console.WriteLine("e1 <--> e3: " + e1.CosineSimilarity(e3));
        Console.WriteLine("e2 <--> e3: " + e2.CosineSimilarity(e3));
        Assert.True(e1.CosineSimilarity(e2) > 0.8);
        Assert.True(e1.CosineSimilarity(e3) < 0.8);
        Assert.True(e2.CosineSimilarity(e3) < 0.8);
    }
}
