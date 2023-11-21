// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using FunctionalTests.TestHelpers;
using Microsoft.KernelMemory;
using Xunit.Abstractions;

namespace FunctionalTests.Service;

// ReSharper disable InconsistentNaming
public class ImportFilesTest : BaseTestCase
{
    private readonly IKernelMemory _memory;
    private readonly string? _fixturesPath;

    public ImportFilesTest(
        IConfiguration cfg,
        ITestOutputHelper output) : base(cfg, output)
    {
        this._fixturesPath = FindFixturesDir();
        Assert.NotNull(this._fixturesPath);
        Console.WriteLine($"\n# Fixtures directory found: {this._fixturesPath}");

        var apiKey = this.Configuration.GetSection("ServiceAuthorization").GetValue<string>("AccessKey");
        this._memory = new MemoryWebClient("http://127.0.0.1:9001/", apiKey: apiKey);
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
