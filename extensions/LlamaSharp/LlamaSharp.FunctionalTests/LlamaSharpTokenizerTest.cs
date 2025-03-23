// Copyright (c) Microsoft. All rights reserved.

using LLama;
using LLama.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory.AI.LlamaSharp;
using Microsoft.KM.TestHelpers;

namespace Microsoft.LlamaSharp.FunctionalTests;

public sealed class LlamaSharpTokenizerTest : BaseFunctionalTestCase
{
    private readonly LlamaSharpTokenizer _target;

    public LlamaSharpTokenizerTest(
        IConfiguration cfg,
        ITestOutputHelper output) : base(cfg, output)
    {
        this.LlamaSharpConfig.Validate();

        var modelFilename = this.LlamaSharpConfig.TextModel.ModelPath.Split('/').Last().Split('\\').Last();
        Console.WriteLine($"Model in use: {modelFilename}");

        var parameters = new ModelParams(this.LlamaSharpConfig.TextModel.ModelPath)
        {
            ContextSize = this.LlamaSharpConfig.TextModel.MaxTokenTotal,
            GpuLayerCount = this.LlamaSharpConfig.TextModel.GpuLayerCount ?? 20,
        };

        LLamaWeights model = LLamaWeights.LoadFromFile(parameters);
        LLamaContext context = model.CreateContext(parameters);

        this._target = new LlamaSharpTokenizer(context);
    }

    [Fact]
    [Trait("Category", "LlamaSharp")]
    public void ItCountsTokens()
    {
        const string text = "{'bos_token': '<|endoftext|>',\n 'eos_token': '<|endoftext|>',\n 'unk_token': '<|endoftext|>'}";

        // Expected result with Phi-3-mini-4k-instruct-q4.gguf, without BoS (https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf)
        Assert.Equal(28, this._target.CountTokens(text));
    }

    [Fact]
    [Trait("Category", "LlamaSharp")]
    public void ItTokenizes()
    {
        const string text = "Let's tokenize this (English) sentence!";
        IReadOnlyList<string> tokens = this._target.GetTokens(text);

        // Expected result using Phi-3-mini-4k-instruct-q4.gguf (https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf)
        Assert.Equal(" Let|'|s| token|ize| this| (|English|)| sentence|!", string.Join('|', tokens));
    }
}
