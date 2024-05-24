// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.AI.TikToken;
using Microsoft.KM.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.TikToken.UnitTests;

public class TikTokenTokenizers : BaseUnitTestCase
{
    public TikTokenTokenizers(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "AI")]
    public void TheyCountTokens()
    {
        const string text = "{'bos_token': '<|endoftext|>',\n 'eos_token': '<|endoftext|>',\n 'unk_token': '<|endoftext|>'}";

        Assert.Equal(47, new DefaultGPTTokenizer().CountTokens(text));
        Assert.Equal(29, new TikTokenGPT2Tokenizer().CountTokens(text));
        Assert.Equal(29, new TikTokenGPT3Tokenizer().CountTokens(text));
        Assert.Equal(21, new TikTokenGPT4Tokenizer().CountTokens(text));
    }
}
