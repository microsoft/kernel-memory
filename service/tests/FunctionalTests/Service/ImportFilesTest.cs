// Copyright (c) Microsoft. All rights reserved.

using FunctionalTests.TestHelpers;
using Microsoft.KernelMemory;
using Xunit.Abstractions;

namespace FunctionalTests.Service;

public class ImportFilesTest : BaseTestCase
{
    private readonly IKernelMemory _memory;
    private readonly string? _fixturesPath;

    public ImportFilesTest(
        IConfiguration cfg,
        ITestOutputHelper output) : base(cfg, output)
    {
        this._fixturesPath = this.FindFixturesDir();
        Assert.NotNull(this._fixturesPath);
        Console.WriteLine($"\n# Fixtures directory found: {this._fixturesPath}");

        this._memory = this.GetMemoryWebClient();
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
            document: new Document(id: "Doc2.txt")
                .AddFile(Path.Join(this._fixturesPath, "Doc1.txt"))
                .AddFile(Path.Join(this._fixturesPath, "Documents", "Doc1.txt")),
            steps: new[] { "extract", "partition" });
    }
}
