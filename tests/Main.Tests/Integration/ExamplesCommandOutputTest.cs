// Copyright (c) Microsoft. All rights reserved.

namespace KernelMemory.Main.Tests.Integration;

/// <summary>
/// Test that executes 'km examples' and verifies output contains expected sections.
/// Uses bash execution to provide proper TTY for Spectre.Console.
/// </summary>
public sealed class ExamplesCommandOutputTest
{
    [Fact]
    public void KmExamples_ExecutesAndOutputsAllSections()
    {
        // Arrange
        var testAssemblyPath = typeof(ExamplesCommandOutputTest).Assembly.Location;
        var testBinDir = Path.GetDirectoryName(testAssemblyPath)!;
        var solutionRoot = Path.GetFullPath(Path.Combine(testBinDir, "../../../../.."));
        var kmDll = Path.Combine(solutionRoot, "src/Main/bin/Debug/net10.0/KernelMemory.Main.dll");
        var outputFile = Path.Combine(Path.GetTempPath(), $"km-examples-test-{Guid.NewGuid():N}.txt");

        Assert.True(File.Exists(kmDll), $"KernelMemory.Main.dll not found at {kmDll}");

        try
        {
            // Act: Execute km examples via bash and capture output
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"dotnet \\\"{kmDll}\\\" examples > \\\"{outputFile}\\\" 2>&1\"",
                UseShellExecute = false
            });

            Assert.NotNull(process);
            process.WaitForExit();

            // Assert: Command succeeded
            Assert.Equal(0, process.ExitCode);
            Assert.True(File.Exists(outputFile), "Output file not created");

            var output = File.ReadAllText(outputFile);

            // Verify output is substantial
            Assert.True(output.Length > 1000, $"Output too short: {output.Length} chars");

            // Verify key sections are present (case-insensitive due to ANSI formatting)
            Assert.Contains("Quick Start Guide", output);
            Assert.Contains("save", output, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("search", output, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("list", output, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("get", output, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("delete", output, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("nodes", output, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("config", output, StringComparison.OrdinalIgnoreCase);

            // Verify search examples are present
            Assert.Contains("km search", output);
            Assert.Contains("docker AND kubernetes", output);
            Assert.Contains("python OR javascript", output);
            Assert.Contains("title:", output);
            Assert.Contains("content:", output);
            Assert.Contains("--limit", output);
            Assert.Contains("--min-relevance", output);

            // Verify MongoDB JSON examples
            Assert.Contains("MongoDB JSON", output);
            Assert.Contains("$and", output);
            Assert.Contains("$text", output);

            // Verify put examples
            Assert.Contains("km put", output);
            Assert.Contains("--tags", output);

            // Count example commands
            var searchCount = System.Text.RegularExpressions.Regex.Matches(output, "km search").Count;
            var putCount = System.Text.RegularExpressions.Regex.Matches(output, "km put").Count;

            Assert.True(searchCount >= 15, $"Expected >= 15 search examples, found {searchCount}");
            Assert.True(putCount >= 5, $"Expected >= 5 put examples, found {putCount}");
        }
        finally
        {
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }
        }
    }
}
