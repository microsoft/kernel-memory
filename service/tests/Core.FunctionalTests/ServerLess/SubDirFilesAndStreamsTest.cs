// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Microsoft.KM.TestHelpers;
using Xunit.Abstractions;

namespace Microsoft.KM.Core.FunctionalTests.ServerLess;

public class SubDirFilesAndStreamsTest : BaseFunctionalTestCase
{
    private readonly IKernelMemory _memory;
    private readonly string? _fixturesPath;

    public SubDirFilesAndStreamsTest(
        IConfiguration cfg,
        ITestOutputHelper output) : base(cfg, output)
    {
        this._fixturesPath = this.FindFixturesDir();
        Assert.NotNull(this._fixturesPath);
        Console.WriteLine($"\n# Fixtures directory found: {this._fixturesPath}");

        this._memory = new KernelMemoryBuilder()
            .WithOpenAI(this.OpenAiConfig)
            // Store data in memory
            .WithSimpleFileStorage(new SimpleFileStorageConfig { StorageType = FileSystemTypes.Volatile })
            .WithSimpleVectorDb(new SimpleVectorDbConfig { StorageType = FileSystemTypes.Volatile })
            .Build<MemoryServerless>();
    }

    [Fact]
    [Trait("Category", "Serverless")]
    public async Task ItImportsFromSubDirsApi1()
    {
        // Act - Assert no exception occurs
        await this._memory.ImportDocumentAsync(
            filePath: Path.Join(this._fixturesPath, "Doc1.txt"),
            documentId: "Doc1.txt",
            steps: ["extract", "partition"]);

        await this._memory.ImportDocumentAsync(
            filePath: Path.Join(this._fixturesPath, "Documents", "Doc1.txt"),
            documentId: "Documents-Doc1.txt",
            steps: ["extract", "partition"]);
    }

    [Fact]
    [Trait("Category", "Serverless")]
    public async Task ItImportsFromSubDirsApi2()
    {
        // Act - Assert no exception occurs
        await this._memory.ImportDocumentAsync(
            document: new Document("Doc2.txt")
                .AddFile(Path.Join(this._fixturesPath, "Doc1.txt"))
                .AddFile(Path.Join(this._fixturesPath, "Documents", "Doc1.txt")),
            steps: ["extract", "partition"]);
    }

    [Fact]
    [Trait("Category", "Serverless")]
    public async Task ItImportsStreams()
    {
        // Arrange
        var fileName = "Doc1.txt";
        var filePath = Path.Join(this._fixturesPath, fileName);
        using MemoryStream memoryStream = new();
        using Stream fileStream = File.OpenRead(filePath);
        await fileStream.CopyToAsync(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        // Act - Assert no exception occurs
        await this._memory.ImportDocumentAsync(
            content: memoryStream,
            documentId: "487BC53B60CFBD42167A0488A78347929E0FE811FC705A94253E419CA5911360",
            fileName: fileName,
            steps: ["extract", "partition"],
            tags: new() { { "user", "user1" } });
    }

    [Fact]
    [Trait("Category", "Serverless")]
    public async Task ItAllowsToDownloadFilesWithTheSameName()
    {
        var doc = new Document { Id = "clones" };
        doc.AddFile(Path.Join(this._fixturesPath, "Doc1.txt"));
        doc.AddFile(Path.Join(this._fixturesPath, "Documents", "Doc1.txt"));

        // Act
        await this._memory.ImportDocumentAsync(document: doc);
        SearchResult result = await this._memory.SearchAsync("Document one");

        // Assert
        Assert.Equal(2, result.Results.Count);
        Assert.NotEqual(result.Results[0].SourceUrl, result.Results[1].SourceUrl);
        Assert.EndsWith("Doc1.txt", result.Results[0].SourceUrl);
        Assert.EndsWith("Doc1.txt", result.Results[1].SourceUrl);
    }
}
