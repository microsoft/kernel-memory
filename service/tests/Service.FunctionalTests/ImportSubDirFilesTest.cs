// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;
using Microsoft.KM.TestHelpers;

namespace Microsoft.KM.Service.FunctionalTests;

public class ImportSubDirFilesTest : BaseFunctionalTestCase
{
    private readonly IKernelMemory _memory;
    private readonly string? _fixturesPath;

    public ImportSubDirFilesTest(
        IConfiguration cfg,
        ITestOutputHelper output) : base(cfg, output)
    {
        this._fixturesPath = this.FindFixturesDir();
        Assert.NotNull(this._fixturesPath);
        Console.WriteLine($"\n# Fixtures directory found: {this._fixturesPath}");

        this._memory = this.GetMemoryWebClient();
    }

    [Fact]
    [Trait("Category", "WebService")]
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
    [Trait("Category", "WebService")]
    public async Task ItImportsFromSubDirsApi2()
    {
        // Act - Assert no exception occurs
        await this._memory.ImportDocumentAsync(
            document: new Document(id: "Doc2.txt")
                .AddFile(Path.Join(this._fixturesPath, "Doc1.txt"))
                .AddFile(Path.Join(this._fixturesPath, "Documents", "Doc1.txt")),
            steps: ["extract", "partition"]);
    }
}
