// Copyright (c) Microsoft. All rights reserved.

using LLama;
using LLama.Common;
using Microsoft.KernelMemory.AI.LlamaSharp;
using Microsoft.KM.TestHelpers;
using Xunit.Abstractions;

namespace Microsoft.LlamaSharp.FunctionalTests;

public sealed class LLamaSharpTokenizerTest : BaseFunctionalTestCase
{
    private readonly LLamaSharpTokenizer _target;

    public LLamaSharpTokenizerTest(
        IConfiguration cfg,
        ITestOutputHelper output) : base(cfg, output)
    {
        this.LlamaSharpConfig.Validate();

        var modelFilename = this.LlamaSharpConfig.ModelPath.Split('/').Last().Split('\\').Last();
        Console.WriteLine($"Model in use: {modelFilename}");

        var parameters = new ModelParams(this.LlamaSharpConfig.ModelPath)
        {
            ContextSize = this.LlamaSharpConfig.MaxTokenTotal,
            GpuLayerCount = this.LlamaSharpConfig.GpuLayerCount ?? 20,
        };

        LLamaWeights model = LLamaWeights.LoadFromFile(parameters);
        LLamaContext context = model.CreateContext(parameters);

        this._target = new LLamaSharpTokenizer(context);
    }

    [Fact]
    public void ItCountsTokens()
    {
        const string text = "{'bos_token': '<|endoftext|>',\n 'eos_token': '<|endoftext|>',\n 'unk_token': '<|endoftext|>'}";

        // Expected result using Phi-3-mini-4k-instruct-q4.gguf (https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf)
        Assert.Equal(29, this._target.CountTokens(text));
    }

    [Fact]
    public void ItTokenizes()
    {
        const string text = "Let's tokenize this (English) sentence!";
        IReadOnlyList<string> tokens = this._target.GetTokens(text);

        // Expected result using Phi-3-mini-4k-instruct-q4.gguf (https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf)
        Assert.Equal("| Let|'|s| token|ize| this| (|English|)| sentence|!", string.Join('|', tokens));
    }
}
