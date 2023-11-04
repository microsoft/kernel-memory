// Copyright (c) Microsoft. All rights reserved.

// ReSharper disable InconsistentNaming

using System.Reflection;
using FunctionalTests.TestHelpers;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.ContentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Xunit.Abstractions;

namespace FunctionalTests.ServerLess;

public class ImportFilesTest : BaseTestCase
{
    private readonly IKernelMemory _memory;
    private readonly string? _fixturesPath;

    public ImportFilesTest(ITestOutputHelper output) : base(output)
    {
        this._fixturesPath = FindFixturesDir();
        Assert.NotNull(this._fixturesPath);
        Console.WriteLine($"\n# Fixtures directory found: {this._fixturesPath}");

        // Save uploaded docs inside this project, under /tmp
        var tmpPath = Path.GetFullPath(Path.Join(this._fixturesPath, "..", "tmp"));
        Console.WriteLine($"Saving temp files in: {tmpPath}");

        this._memory = new KernelMemoryBuilder()
            .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"))
            // Store data in memory
            .WithSimpleFileStorage(new SimpleFileStorageConfig { StorageType = FileSystemTypes.Volatile })
            .WithSimpleVectorDb(new SimpleVectorDbConfig { StorageType = FileSystemTypes.Volatile })
            .BuildServerlessClient();
    }

    [Fact]
    public async Task ItImportsFromSubDirsApi1()
    {
        // Act - Assert no exception occurs
        await this._memory.ImportDocumentAsync(
            filePath: Path.Join(this._fixturesPath, "Doc1.txt"),
            documentId: "Doc1.txt",
            steps: new[] { "extract", "partition" });

        await this._memory.ImportDocumentAsync(
            filePath: Path.Join(this._fixturesPath, "Documents", "Doc1.txt"),
            documentId: "Documents-Doc1.txt",
            steps: new[] { "extract", "partition" });
    }

    [Fact]
    public async Task ItImportsFromSubDirsApi2()
    {
        // Act - Assert no exception occurs
        await this._memory.ImportDocumentAsync(
            document: new Document("Doc2.txt")
                .AddFile(Path.Join(this._fixturesPath, "Doc1.txt"))
                .AddFile(Path.Join(this._fixturesPath, "Documents", "Doc1.txt")),
            steps: new[] { "extract", "partition" });
    }

    [Fact]
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
            steps: new[] { "extract", "partition" },
            tags: new() { { "user", "user1" } });
    }

    // Find the "Fixtures" directory (inside the project)
    private static string? FindFixturesDir()
    {
        // start from the location of the executing assembly, and traverse up max 5 levels
        var path = Path.GetDirectoryName(Path.GetFullPath(Assembly.GetExecutingAssembly().Location));
        for (var i = 0; i < 5; i++)
        {
            Console.WriteLine($"Checking '{path}'");
            var test = Path.Join(path, "Fixtures");
            if (Directory.Exists(test)) { return test; }

            // up one level
            path = Path.GetDirectoryName(path);
        }

        return null;
    }
}
