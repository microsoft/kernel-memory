using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.ContentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Xunit.Abstractions;

namespace Redis.FunctionalTests;

public class IndexCreationTest : BaseTestCase
{
    public IndexCreationTest(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
    }

    [Fact]
    [Trait("Category", "RedisFunctionalTest")]
    public async Task ItNormalizesIndexNames()
    {
        // Arrange
        string indexNameWithDashes = "name-with-dashes";
        string indexNameWithUnderscores = "name_with_underscore";

        var memory = new KernelMemoryBuilder()
            .WithRedisMemoryDb(this.RedisConfiguration)
            .WithSimpleFileStorage(new SimpleFileStorageConfig { StorageType = FileSystemTypes.Volatile, Directory = "_files" })
            .WithOpenAITextGeneration(this.OpenAIConfiguration)
            .WithOpenAITextEmbeddingGeneration(this.OpenAIConfiguration)
            .Build();

        // Act - Assert no exception occurs
        await memory.ImportTextAsync("something", index: indexNameWithDashes);
        await memory.ImportTextAsync("something", index: indexNameWithUnderscores);

        // Cleanup
        await memory.DeleteIndexAsync(indexNameWithDashes);
        await memory.DeleteIndexAsync(indexNameWithUnderscores);
    }

    [Fact]
    [Trait("Category", "RedisFunctionalTest")]
    public async Task ItListsIndexes()
    {
        // Arrange
        string indexNameWithDashes = "name-with-dashes";
        string indexNameWithUnderscores = "name_with_underscore";
        string indexNameWithUnderscoresNormalized = "name-with-underscore";

        var memory = new KernelMemoryBuilder()
            .WithRedisMemoryDb(this.RedisConfiguration)
            .WithSimpleFileStorage(new SimpleFileStorageConfig { StorageType = FileSystemTypes.Volatile, Directory = "_files" })
            .WithOpenAITextGeneration(this.OpenAIConfiguration)
            .WithOpenAITextEmbeddingGeneration(this.OpenAIConfiguration)
            .Build();

        // Act
        await memory.ImportTextAsync("something", index: indexNameWithDashes);
        await memory.ImportTextAsync("something", index: indexNameWithUnderscores);
        var list = (await memory.ListIndexesAsync()).ToList();

        // Clean up before exceptions can occur
        await memory.DeleteIndexAsync(indexNameWithDashes);
        await memory.DeleteIndexAsync(indexNameWithUnderscores);

        // Assert
        Assert.True(list.Any(x => x.Name == indexNameWithDashes));
        Assert.False(list.Any(x => x.Name == indexNameWithUnderscores));
        Assert.True(list.Any(x => x.Name == indexNameWithUnderscoresNormalized));
    }

    [Fact]
    [Trait("Category", "RedisFunctionalTest")]
    public async Task ItUsesDefaultIndex()
    {
        // Arrange
        string emptyIndexName = "";

        var memory = new KernelMemoryBuilder()
            .WithRedisMemoryDb(this.RedisConfiguration)
            .WithSimpleFileStorage(new SimpleFileStorageConfig { StorageType = FileSystemTypes.Volatile, Directory = "_files" })
            .WithOpenAITextGeneration(this.OpenAIConfiguration)
            .WithOpenAITextEmbeddingGeneration(this.OpenAIConfiguration)
            .Build();

        // Act
        await memory.ImportTextAsync("something", index: emptyIndexName);
        var list = (await memory.ListIndexesAsync()).ToList();

        // Clean up before exceptions can occur
        await memory.DeleteIndexAsync(emptyIndexName);

        // Assert
        Assert.True(list.Any(x => x.Name == "default"));
    }
}
