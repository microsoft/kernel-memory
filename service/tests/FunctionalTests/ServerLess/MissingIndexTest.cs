// Copyright (c) Microsoft. All rights reserved.

using FunctionalTests.TestHelpers;
using Microsoft.KernelMemory;
using Xunit.Abstractions;

namespace FunctionalTests.ServerLess;

public class MissingIndexTest : BaseTestCase
{
    public MissingIndexTest(
        IConfiguration cfg,
        ITestOutputHelper output) : base(cfg, output)
    {
    }

    [Theory]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    [InlineData("qdrant")]
    [InlineData("az_ai_search")]
    public async Task ItHandlesMissingIndexesConsistently(string memoryType)
    {
        // Arrange
        var memory = this.GetServerlessMemory(memoryType);
        string indexName = Guid.NewGuid().ToString("D");
        await memory.DeleteIndexAsync(indexName);

        // Act: verify the index doesn't exist
        IEnumerable<IndexDetails> list = await memory.ListIndexesAsync();
        Assert.False(list.Any(x => x.Name == indexName));

        // Act: Delete a non-existing index, no exception
        await memory.DeleteIndexAsync(indexName);

        // Act: Search a non-existing index
        var answer = await memory.AskAsync("What's the number after 9?", index: indexName);
        Assert.Equal(NotFound, answer.Result);

        // Act: Search a non-existing index
        var searchResult = await memory.SearchAsync("some query", index: indexName);
        Assert.Equal(0, searchResult.Results.Count);

        // Act: delete doc from non existing index
        await memory.DeleteDocumentAsync(documentId: Guid.NewGuid().ToString("D"), index: indexName);

        // Act: get status of non existing doc/index
        bool isReady = await memory.IsDocumentReadyAsync(documentId: Guid.NewGuid().ToString("D"), index: indexName);
        Assert.Equal(false, isReady);

        // Act: get status of non existing doc/index
        DataPipelineStatus? status = await memory.GetDocumentStatusAsync(documentId: Guid.NewGuid().ToString("D"), index: indexName);
        Assert.Null(status);

        // Assert: verify the index doesn't exist yet
        list = await memory.ListIndexesAsync();
        Assert.False(list.Any(x => x.Name == indexName));

        // Act: import into a non existing index - the index is created
        var id = await memory.ImportTextAsync("some text", documentId: "foo", index: indexName);
        Assert.NotEmpty(id);

        // Assert: verify the index has been created
        list = await memory.ListIndexesAsync();
        Assert.True(list.Any(x => x.Name == indexName));

        // clean up
        await memory.DeleteIndexAsync(indexName);
    }
}
