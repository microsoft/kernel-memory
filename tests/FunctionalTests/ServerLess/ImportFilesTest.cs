// Copyright (c) Microsoft. All rights reserved.

// ReSharper disable InconsistentNaming

using System.Reflection;
using FunctionalTests.TestHelpers;
using Microsoft.SemanticMemory;
using Xunit.Abstractions;

namespace FunctionalTests.ServerLess;

public class ImportFilesTest : BaseTestCase
{
    private readonly ISemanticMemoryClient _memory;

    public ImportFilesTest(ITestOutputHelper output) : base(output)
    {
        this._memory = new MemoryClientBuilder()
            .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"))
            .BuildServerlessClient();
    }

    [Fact]
    public async Task ItImportsFromSubDirs()
    {
        // Arrange
        var path = FindRootDir();
        Assert.NotNull(path);
        Console.WriteLine($"\n# Directory found: {path}");

        // Act - Assert no exception occurs
        await this._memory.ImportDocumentAsync(filePath: Path.Join(path, @"Doc1.txt"), documentId: "Doc1.txt", steps: new[] { "extract", "partition" });
        await this._memory.ImportDocumentAsync(filePath: Path.Join(path, @"Documents\Doc1.txt"), documentId: @"Documents\Doc1.txt", steps: new[] { "extract", "partition" });
    }

    // Find the root directory (of the project)
    private static string? FindRootDir()
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
