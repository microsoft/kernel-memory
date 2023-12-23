// Copyright (c) Microsoft. All rights reserved.

using FunctionalTests3P.TestHelpers;
using Xunit.Abstractions;

namespace FunctionalTests3P;

public class IndexDeletionTest : BaseTestCase
{
    public IndexDeletionTest(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
    }

    [Theory]
    [InlineData("postgres")]
    [InlineData("simple_volatile")]
    [InlineData("az_ai_search")]
    public async Task ItDeletesIndexes(string memoryType)
    {
        // Arrange
        var memory = this.GetServerlessMemory(memoryType);

        // Act
        await memory.ImportTextAsync(
            text: "this is a test",
            documentId: "text1",
            index: "index1",
            steps: new[] { "extract", "partition", "gen_embeddings", "save_records" });

        await memory.ImportTextAsync(
            text: "this is a test",
            documentId: "text2",
            index: "index1",
            steps: new[] { "extract", "partition", "gen_embeddings", "save_records" });

        await memory.ImportTextAsync(
            text: "this is a test",
            documentId: "text3",
            index: "index2",
            steps: new[] { "extract", "partition", "gen_embeddings", "save_records" });

        await memory.ImportTextAsync(
            text: "this is a test",
            documentId: "text4",
            index: "index2",
            steps: new[] { "extract", "partition", "gen_embeddings", "save_records" });

        // Assert (no exception occurs, manual verification of collection being deleted)
        await memory.DeleteDocumentAsync(documentId: "text1", index: "index1");
        await memory.DeleteIndexAsync(index: "index2");
    }
}
