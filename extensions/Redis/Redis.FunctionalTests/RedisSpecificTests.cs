// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.TestHelpers;
using Xunit.Abstractions;

namespace Redis.FunctionalTests;

public class RedisSpecificTests : BaseFunctionalTestCase
{
    private readonly MemoryServerless _memory;

    public RedisSpecificTests(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
        Assert.False(string.IsNullOrEmpty(this.OpenAiConfig.APIKey));

        this._memory = new KernelMemoryBuilder()
            .WithSearchClientConfig(new SearchClientConfig { EmptyAnswer = NotFound })
            .WithOpenAI(this.OpenAiConfig)
            .WithRedisMemoryDb(this.RedisConfig)
            .Build<MemoryServerless>();
    }

    [Fact]
    [Trait("Category", "Redis")]
    public async Task ItThrowsWhenAttemptingToInsertUnIndexedTag()
    {
        var res = await Assert.ThrowsAsync<ArgumentException>(() => this._memory.ImportTextAsync(
            "Text that may never be used because we have an invalid tag in here",
            documentId: "1",
            tags: new TagCollection { { "non-configuredTag", "foobarbaz" } }));

        Assert.Equal(
            "Attempt to insert un-indexed tag field: non-configuredTag, will not be able to filter on it, please adjust the tag settings in your Redis Configuration",
            res.Message);
    }

    [Fact]
    [Trait("Category", "Redis")]
    public async Task ItThrowsWhenAttemptingToInsertTagWithSeparator()
    {
        var res = await Assert.ThrowsAsync<ArgumentException>(() => this._memory.ImportTextAsync(
            "Text that may never be used because we have an invalid tag in here",
            documentId: "1",
            tags: new TagCollection { { "user", "foo|bar|baz" } }));
        Assert.Equal(
            $"Attempted to insert record with tag field: user containing the separator: '|'. Update your {nameof(Microsoft.KernelMemory.RedisConfig)} to use a different separator, or remove the separator from the field.",
            res.Message);
    }
}
