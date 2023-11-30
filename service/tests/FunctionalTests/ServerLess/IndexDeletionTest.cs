// Copyright (c) Microsoft. All rights reserved.

using FunctionalTests.TestHelpers;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.ContentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Xunit.Abstractions;

namespace FunctionalTests.ServerLess;

// ReSharper disable InconsistentNaming
public class IndexDeletionTest : BaseTestCase
{
    public IndexDeletionTest(
        IConfiguration cfg,
        ITestOutputHelper output) : base(cfg, output)
    {
    }

    [Fact]
    public async Task ItDeletesSimpleVectorDbIndexes()
    {
        // Arrange
        var openAIKey = this.OpenAIConfiguration.GetValue<string>("APIKey")
                        ?? throw new TestCanceledException("OpenAI API key is missing");
        var memory = new KernelMemoryBuilder()
            .WithOpenAIDefaults(openAIKey)
            .WithSimpleFileStorage(new SimpleFileStorageConfig { Directory = "tmp-files" })
            .WithSimpleVectorDb(new SimpleVectorDbConfig { Directory = "tmp-vectors", StorageType = FileSystemTypes.Disk })
            .Build<MemoryServerless>();

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

        // Assert (no exception occurs, manual verification of folders being deleted)
        await memory.DeleteDocumentAsync(documentId: "text1", index: "index1");
        await memory.DeleteIndexAsync(index: "index2");
    }

    [Fact]
    public async Task ItDeletesQdrantIndexes()
    {
        // Arrange
        var openAIKey = this.OpenAIConfiguration.GetValue<string>("APIKey")
                        ?? throw new TestCanceledException("OpenAI API key is missing");

        var memory = new KernelMemoryBuilder()
            .WithOpenAIDefaults(openAIKey)
            .WithSimpleFileStorage(new SimpleFileStorageConfig { Directory = "tmp-files" })
            .WithQdrant("http://127.0.0.1:6333")
            .Build<MemoryServerless>();

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
