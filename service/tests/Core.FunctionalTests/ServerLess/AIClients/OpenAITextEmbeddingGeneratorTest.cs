// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KM.TestHelpers;
using Xunit.Abstractions;

namespace Microsoft.KM.Core.FunctionalTests.ServerLess.AIClients;

// ReSharper disable StringLiteralTypo
public sealed class OpenAITextEmbeddingGeneratorTest : BaseFunctionalTestCase
{
    private readonly OpenAITextEmbeddingGenerator _target;

    public OpenAITextEmbeddingGeneratorTest(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
        var config = this.OpenAiConfig;
        config.EmbeddingModel = "text-embedding-ada-002";
        this._target = new OpenAITextEmbeddingGenerator(config, loggerFactory: null);
    }

    [Fact]
    [Trait("Category", "Serverless")]
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
