// Copyright (c) Microsoft. All rights reserved.

// ReSharper disable InconsistentNaming

using FunctionalTests.TestHelpers;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.ContentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Xunit.Abstractions;

namespace FunctionalTests.ServerLess;

public class IndexDeletionTest : BaseTestCase
{
    public IndexDeletionTest(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task ItDeletesSimpleVectorDbIndexes()
    {
        // Arrange
        var memory = new KernelMemoryBuilder()
            .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"))
            .WithSimpleFileStorage(new SimpleFileStorageConfig { Directory = "tmp-files" })
            .WithSimpleVectorDb(new SimpleVectorDbConfig { Directory = "tmp-vectors", StorageType = FileSystemTypes.Disk })
            .BuildServerlessClient();

        // Act
        await memory.ImportTextAsync(
            text: "this is a test",
            documentId: "text1",
            index: "index1",
            steps: new[] { "extract", "partition", "gen_embeddings", "save_embeddings" });

        await memory.ImportTextAsync(
            text: "this is a test",
            documentId: "text2",
            index: "index1",
            steps: new[] { "extract", "partition", "gen_embeddings", "save_embeddings" });

        await memory.ImportTextAsync(
            text: "this is a test",
            documentId: "text3",
            index: "index2",
            steps: new[] { "extract", "partition", "gen_embeddings", "save_embeddings" });

        await memory.ImportTextAsync(
            text: "this is a test",
            documentId: "text4",
            index: "index2",
            steps: new[] { "extract", "partition", "gen_embeddings", "save_embeddings" });

        // Assert (no exception occurs, manual verification of folders being deleted)
        await memory.DeleteDocumentAsync(documentId: "text1", index: "index1");
        await memory.DeleteIndexAsync(index: "index2");
    }

    [Fact]
    public async Task ItDeletesQdrantIndexes()
    {
        // Arrange
        var memory = new KernelMemoryBuilder()
            .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"))
            .WithSimpleFileStorage(new SimpleFileStorageConfig { Directory = "tmp-files" })
            .WithQdrant("http://127.0.0.1:6333")
            .BuildServerlessClient();

        // Act
        await memory.ImportTextAsync(
            text: "this is a test",
            documentId: "text1",
            index: "index1",
            steps: new[] { "extract", "partition", "gen_embeddings", "save_embeddings" });

        await memory.ImportTextAsync(
            text: "this is a test",
            documentId: "text2",
            index: "index1",
            steps: new[] { "extract", "partition", "gen_embeddings", "save_embeddings" });

        await memory.ImportTextAsync(
            text: "this is a test",
            documentId: "text3",
            index: "index2",
            steps: new[] { "extract", "partition", "gen_embeddings", "save_embeddings" });

        await memory.ImportTextAsync(
            text: "this is a test",
            documentId: "text4",
            index: "index2",
            steps: new[] { "extract", "partition", "gen_embeddings", "save_embeddings" });

        // Assert (no exception occurs, manual verification of collection being deleted)
        await memory.DeleteDocumentAsync(documentId: "text1", index: "index1");
        await memory.DeleteIndexAsync(index: "index2");
    }
}
