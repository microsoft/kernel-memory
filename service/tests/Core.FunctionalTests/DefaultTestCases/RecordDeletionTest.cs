// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.MemoryStorage;

namespace Microsoft.KM.Core.FunctionalTests.DefaultTestCases;

public static class RecordDeletionTest
{
    public static async Task ItDeletesRecords(IKernelMemory memory, IMemoryDb db, Action<string> log)
    {
        // Arrange
        const string DocId = "ItDeletesRecords-file2-largePDF.pdf";
        const string IndexName = "default";

        log("Uploading document");
        await memory.ImportDocumentAsync(
            "file2-largePDF.pdf",
            documentId: DocId,
            index: IndexName,
            steps: Constants.PipelineWithoutSummary);

        var count = 0;
        while (!await memory.IsDocumentReadyAsync(index: IndexName, documentId: DocId))
        {
            Assert.True(count++ <= 30, "Document import timed out");
            log("Waiting for memory ingestion to complete...");
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        // Act: Load and delete records
        log("Fetching records to delete");
        IAsyncEnumerable<MemoryRecord> records = db.GetListAsync(
            index: IndexName,
            limit: -1,
            filters: new List<MemoryFilter> { MemoryFilters.ByDocument(DocId) });

        log("Deleting records");
        count = 0;
        await foreach (var record in records.ConfigureAwait(false))
        {
            count++;
            await db.DeleteAsync(index: IndexName, record).ConfigureAwait(false);
        }

        log($"{count} records deleted");

        // Cleanup
        log("Deleting test document");
        await memory.DeleteDocumentAsync(DocId);
    }
}
