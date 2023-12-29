// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.ContentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Postgres.FunctionalTests.TestHelpers;

namespace Postgres.FunctionalTests;

public class IndexCreationTests : BaseTestCase
{
    public IndexCreationTests(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
    }

    [Fact]
    [Trait("Category", "PostgresFunctionalTest")]
    public async Task ItNormalizesIndexNames()
    {
        // Arrange
        string indexNameWithDashes = "name-with-dashes";
        string indexNameWithUnderscores = "name_with_underscore";

        var memory = new KernelMemoryBuilder()
            .WithPostgresMemoryDb(this.PostgresConfiguration)
            .WithSimpleFileStorage(new SimpleFileStorageConfig { StorageType = FileSystemTypes.Volatile, Directory = "_files" })
            // .WithOpenAI(this.OpenAIConfiguration)
            .WithAzureOpenAITextGeneration(this.AzureOpenAITextConfiguration)
            .WithAzureOpenAITextEmbeddingGeneration(this.AzureOpenAIEmbeddingConfiguration)
            .Build();

        // Act - Assert no exception occurs
        await memory.ImportTextAsync("something", index: indexNameWithDashes);
        await memory.ImportTextAsync("something", index: indexNameWithUnderscores);

        // Cleanup
        await memory.DeleteIndexAsync(indexNameWithDashes);
        await memory.DeleteIndexAsync(indexNameWithUnderscores);
    }

    [Theory]
    [InlineData("postgres")]
    [InlineData("simple_volatile")]
    [InlineData("az_ai_search")]
    [Trait("Category", "PostgresFunctionalTest")]
    public async Task ItListsIndexes(string memoryType)
    {
        // Arrange
        string indexNameWithDashes = "name-with-dashes";
        string indexNameWithUnderscores = "name_with_underscore";
        string indexNameWithUnderscoresNormalized = "name-with-underscore";

        var memory = new KernelMemoryBuilder()
            .WithPostgresMemoryDb(this.PostgresConfiguration)
            .WithSimpleFileStorage(new SimpleFileStorageConfig { StorageType = FileSystemTypes.Volatile, Directory = "_files" })
            // .WithOpenAI(this.OpenAIConfiguration)
            .WithAzureOpenAITextGeneration(this.AzureOpenAITextConfiguration)
            .WithAzureOpenAITextEmbeddingGeneration(this.AzureOpenAIEmbeddingConfiguration)
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
}
