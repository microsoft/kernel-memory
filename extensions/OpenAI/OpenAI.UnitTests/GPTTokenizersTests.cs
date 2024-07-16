// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KM.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.OpenAI.UnitTests;

public class GPTTokenizersTests(ITestOutputHelper output) : BaseUnitTestCase(output)
{
    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "AI")]
    public void TheyCountTokens()
    {
        const string text = "{'bos_token': '<|endoftext|>',\n 'eos_token': '<|endoftext|>',\n 'unk_token': '<|endoftext|>'}";

        Assert.Equal(29, new GPT2Tokenizer().CountTokens(text));
        Assert.Equal(29, new GPT3Tokenizer().CountTokens(text));
        Assert.Equal(21, new GPT4Tokenizer().CountTokens(text));
    }
}
