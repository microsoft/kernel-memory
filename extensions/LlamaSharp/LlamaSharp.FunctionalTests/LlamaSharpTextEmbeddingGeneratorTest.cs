// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.LlamaSharp;
using Microsoft.KM.TestHelpers;

namespace Microsoft.LlamaSharp.FunctionalTests;

public sealed class LlamaSharpTextEmbeddingGeneratorTest : BaseFunctionalTestCase
{
    private readonly LlamaSharpTextEmbeddingGenerator _target;

    public LlamaSharpTextEmbeddingGeneratorTest(
        IConfiguration cfg,
        ITestOutputHelper output) : base(cfg, output)
    {
        this.LlamaSharpConfig.Validate();
        this._target = new LlamaSharpTextEmbeddingGenerator(this.LlamaSharpConfig.EmbeddingModel, loggerFactory: null);
        var modelFilename = this.LlamaSharpConfig.TextModel.ModelPath.Split('/').Last().Split('\\').Last();
        Console.WriteLine($"Model in use: {modelFilename}");
    }

    [Fact]
    [Trait("Category", "LlamaSharp")]
    public async Task ItGeneratesEmbeddingVectors()
    {
        // Act
        Embedding embedding = await this._target.GenerateEmbeddingAsync("some text");

        // Assert
        Console.WriteLine("Embedding size: " + embedding.Length);

        // Expected result using nomic-embed-text-v1.5.Q8_0.gguf
        Assert.Equal(768, embedding.Length);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._target.Dispose();
        }

        base.Dispose(disposing);
    }
}
