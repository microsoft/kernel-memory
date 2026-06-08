// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.MemoryStorage;

namespace Microsoft.KM.Core.FunctionalTests.DefaultTestCases;

public static class RecordDeletionTest
{
    public static async Task ItDeletesRecords(IKernelMemory memory, IMemoryDb db, Action<string> log)
    {
        async Task WaitForImport(string indexName, string docId1)
        {
            var count = 0;
            while (!await memory.IsDocumentReadyAsync(index: indexName, documentId: docId1))
            {
                Assert.True(count++ <= 30, "Document import timed out");
                log("Waiting for memory ingestion to complete...");
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        // Arrange
        const string DocId1 = "ItDeletesRecords-file2-largePDF.pdf";
        const string DocId2 = "ItDeletesRecords-file2-largePDF.pdf";
        const string DocId3 = "ItDeletesRecords-file2-largePDF.pdf";
        const string IndexName = "default";

        log("Uploading doc1");
        await memory.ImportDocumentAsync(
            "file2-largePDF.pdf",
            documentId: DocId1,
            index: IndexName,
            steps: Constants.PipelineWithoutSummary);

        log("Uploading doc2");
        await memory.ImportDocumentAsync(
            "file2-largePDF.pdf",
            documentId: DocId2,
            index: IndexName,
            steps: Constants.PipelineWithoutSummary);

        log("Uploading doc3");
        await memory.ImportDocumentAsync(
            "file2-largePDF.pdf",
            documentId: DocId3,
            index: IndexName,
            steps: Constants.PipelineWithoutSummary);

        await WaitForImport(IndexName, DocId1);
        await WaitForImport(IndexName, DocId2);
        await WaitForImport(IndexName, DocId3);

        // Act: Load and delete records
        log("Fetching records to delete");
        IAsyncEnumerable<MemoryRecord> records = db.GetListAsync(
            index: IndexName,
            limit: -1,
            filters:
            [
                MemoryFilters.ByDocument(DocId3),
                MemoryFilters.ByDocument(DocId2),
                MemoryFilters.ByDocument(DocId1),
            ]);

        log("Deleting records");
        var count = 0;
        await foreach (var record in records.ConfigureAwait(false))
        {
            count++;
            await db.DeleteAsync(index: IndexName, record).ConfigureAwait(false);
        }

        log($"{count} records deleted");

        // Cleanup
        log("Deleting test document");
        await memory.DeleteDocumentAsync(DocId3);
        await memory.DeleteDocumentAsync(DocId2);
        await memory.DeleteDocumentAsync(DocId1);
    }
}
