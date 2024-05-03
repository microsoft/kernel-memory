// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;

namespace Microsoft.KM.Core.FunctionalTests.DefaultTestCases;

public static class IndexCreationTest
{
    public static async Task ItDoesntFailIfTheIndexExistsAlready(IKernelMemory memory, Action<string> log)
    {
        // Arrange
        string indexName = Guid.NewGuid().ToString("D");

        // Act - No exception should occur if the pipeline tries to create an index that already exists
        string id1 = await memory.ImportTextAsync("text1", index: indexName, steps: Constants.PipelineWithoutSummary);
        while (!await memory.IsDocumentReadyAsync(documentId: id1, index: indexName))
        {
            log($"[id1: {id1}] Waiting for memory ingestion to complete...");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        string id2 = await memory.ImportTextAsync("text2", index: indexName, steps: Constants.PipelineWithoutSummary);
        while (!await memory.IsDocumentReadyAsync(documentId: id2, index: indexName))
        {
            log($"[id2: {id2}] Waiting for memory ingestion to complete...");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        // Cleanup
        await memory.DeleteIndexAsync(indexName);
    }
}
