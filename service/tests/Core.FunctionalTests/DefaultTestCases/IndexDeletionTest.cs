// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;

namespace Microsoft.KM.Core.FunctionalTests.DefaultTestCases;

public static class IndexDeletionTest
{
    public static async Task ItDeletesIndexes(IKernelMemory memory, Action<string> log)
    {
        // Act
        await memory.ImportTextAsync(
            text: "this is a test",
            documentId: "text1",
            index: "index1",
            steps: ["extract", "partition", "gen_embeddings", "save_records"]);

        await memory.ImportTextAsync(
            text: "this is a test",
            documentId: "text2",
            index: "index1",
            steps: ["extract", "partition", "gen_embeddings", "save_records"]);

        await memory.ImportTextAsync(
            text: "this is a test",
            documentId: "text3",
            index: "index2",
            steps: ["extract", "partition", "gen_embeddings", "save_records"]);

        await memory.ImportTextAsync(
            text: "this is a test",
            documentId: "text4",
            index: "index2",
            steps: ["extract", "partition", "gen_embeddings", "save_records"]);

        // Assert (no exception occurs, manual verification of collection being deleted)
        await memory.DeleteDocumentAsync(documentId: "text1", index: "index1");
        await memory.DeleteIndexAsync(index: "index2");
    }
}
