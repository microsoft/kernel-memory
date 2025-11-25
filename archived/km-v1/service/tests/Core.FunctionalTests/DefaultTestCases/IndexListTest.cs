// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;

namespace Microsoft.KM.Core.FunctionalTests.DefaultTestCases;

public static class IndexListTest
{
    public static async Task ItNormalizesIndexNames(IKernelMemory memory, Action<string> log)
    {
        // Arrange
        string indexNameWithDashes = "name-with-dashes";
        string indexNameWithUnderscores = "name_with_underscore";

        // Act - Assert no exception occurs
        await memory.ImportTextAsync("something", index: indexNameWithDashes);
        await memory.ImportTextAsync("something", index: indexNameWithUnderscores);

        // Cleanup
        await memory.DeleteIndexAsync(indexNameWithDashes);
        await memory.DeleteIndexAsync(indexNameWithUnderscores);
    }

    public static async Task ItUsesDefaultIndexName(IKernelMemory memory, Action<string> log, string expectedDefault)
    {
        // Arrange
        string emptyIndexName = string.Empty;

        // Act
        var id = await memory.ImportTextAsync("something", index: emptyIndexName);
        var count = 0;
        while (!await memory.IsDocumentReadyAsync(id))
        {
            Assert.True(count++ <= 30, "Document import timed out");
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        var list = (await memory.ListIndexesAsync()).ToList();

        // Clean up before exceptions can occur
        await memory.DeleteIndexAsync(emptyIndexName);

        // Assert
        Assert.True(list.Any(x => x.Name == expectedDefault));
    }

    public static async Task ItListsIndexes(IKernelMemory memory, Action<string> log)
    {
        // Arrange
        string indexName1 = Guid.NewGuid().ToString("D");
        string indexName2 = Guid.NewGuid().ToString("D");
        string indexNameWithDashes = "name-with-dashes";
        string indexNameWithUnderscores = "name_with_underscore";
        string indexNameWithUnderscoresNormalized = "name-with-underscore";

        Console.WriteLine("Index 1:" + indexName1);
        Console.WriteLine("Index 2:" + indexName2);
        Console.WriteLine("Index 3:" + indexNameWithDashes);
        Console.WriteLine("Index 4:" + indexNameWithUnderscores);

        string id1 = await memory.ImportTextAsync("text1", index: indexName1, steps: Constants.PipelineWithoutSummary);
        string id2 = await memory.ImportTextAsync("text2", index: indexName2, steps: Constants.PipelineWithoutSummary);
        string id3 = await memory.ImportTextAsync("text3", index: indexNameWithDashes, steps: Constants.PipelineWithoutSummary);
        string id4 = await memory.ImportTextAsync("text4", index: indexNameWithUnderscores, steps: Constants.PipelineWithoutSummary);

        while (!await memory.IsDocumentReadyAsync(documentId: id1, index: indexName1))
        {
            log($"[id1: {id1}] Waiting for memory ingestion to complete...");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        while (!await memory.IsDocumentReadyAsync(documentId: id2, index: indexName2))
        {
            log($"[id2: {id2}] Waiting for memory ingestion to complete...");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        while (!await memory.IsDocumentReadyAsync(documentId: id3, index: indexNameWithDashes))
        {
            log($"[id3: {id3}] Waiting for memory ingestion to complete...");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        while (!await memory.IsDocumentReadyAsync(documentId: id4, index: indexNameWithUnderscores))
        {
            log($"[id4: {id4}] Waiting for memory ingestion to complete...");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        // Act
        List<IndexDetails> list = (await memory.ListIndexesAsync()).ToList();
        Console.WriteLine("Indexes found:");
        foreach (var index in list)
        {
            Console.WriteLine(" - " + index.Name);
        }

        // Clean up before exceptions can occur
        await memory.DeleteIndexAsync(indexName1);
        await memory.DeleteIndexAsync(indexName2);
        await memory.DeleteIndexAsync(indexNameWithDashes);
        await memory.DeleteIndexAsync(indexNameWithUnderscores);

        // Assert
        Assert.True(list.Any(x => x.Name == indexName1));
        Assert.True(list.Any(x => x.Name == indexName2));
        Assert.True(list.Any(x => x.Name == indexNameWithDashes));
        Assert.True(list.Any(x => x.Name == indexNameWithUnderscoresNormalized));
        Assert.False(list.Any(x => x.Name == indexNameWithUnderscores));
    }
}
