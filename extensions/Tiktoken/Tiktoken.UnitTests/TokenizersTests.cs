// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.AI;
using Microsoft.KM.TestHelpers;

namespace Microsoft.Tiktoken.UnitTests;

public class TokenizersTests(ITestOutputHelper output) : BaseUnitTestCase(output)
{
    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "AI")]
    public void CanTokenize()
    {
        const string helloWorld = "hello world";

        var gpt3 = new GPT3Tokenizer();
        var tokens = gpt3.GetTokens(helloWorld);
        Assert.Equal(["hello", " world"], tokens);

        var gpt4 = new GPT4Tokenizer();
        tokens = gpt4.GetTokens(helloWorld);
        Assert.Equal(["hello", " world"], tokens);

        var gpt4o = new GPT4oTokenizer();
        tokens = gpt4o.GetTokens(helloWorld);
        Assert.Equal(["hello", " world"], tokens);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "AI")]
    public void TheyCountTokens()
    {
        const string text = "{'bos_token': '<|endoftext|>',\n 'eos_token': '<|endoftext|>',\n 'unk_token': '<|endoftext|>'}";

        Assert.Equal(29, new GPT3Tokenizer().CountTokens(text));
        Assert.Equal(21, new GPT4Tokenizer().CountTokens(text));
        Assert.Equal(22, new GPT4oTokenizer().CountTokens(text));
    }
}
