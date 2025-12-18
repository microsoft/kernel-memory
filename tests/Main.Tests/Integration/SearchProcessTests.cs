// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics;
using System.Text.Json;
using Xunit.Sdk;

namespace KernelMemory.Main.Tests.Integration;

/// <summary>
/// End-to-end CLI tests using actual process execution.
/// Executes km commands as separate processes and verifies actual JSON output.
/// Tests the COMPLETE path including all CLI layers, formatting, and output.
/// </summary>
public sealed class SearchProcessTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;
    private readonly string _kmPath;

    public SearchProcessTests()
    {
        this._tempDir = Path.Combine(Path.GetTempPath(), $"km-process-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(this._tempDir);

        this._configPath = Path.Combine(this._tempDir, "config.json");

        // Find the km binary (from build output)
        // Get solution root by going up from test assembly location
        var testAssemblyPath = typeof(SearchProcessTests).Assembly.Location;
        var testBinDir = Path.GetDirectoryName(testAssemblyPath)!;
        var solutionRoot = Path.GetFullPath(Path.Combine(testBinDir, "../../../../.."));
        this._kmPath = Path.Combine(solutionRoot, "src/Main/bin/Debug/net10.0/KernelMemory.Main.dll");

        if (!File.Exists(this._kmPath))
        {
            throw new FileNotFoundException($"KernelMemory.Main.dll not found at {this._kmPath}. Run dotnet build first.");
        }
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(this._tempDir))
            {
                Directory.Delete(this._tempDir, true);
            }
        }
        catch (IOException)
        {
            // Ignore cleanup errors
        }
    }

    /// <summary>
    /// Execute km command and return JSON output.
    /// </summary>
    /// <param name="args">Command line arguments to pass to km.</param>
    /// <returns>Standard output from the command.</returns>
    private async Task<string> ExecuteKmAsync(string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{this._kmPath} {args}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start km process");
        }

        var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        var error = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
        await process.WaitForExitAsync().ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"km command failed (exit {process.ExitCode}): {error}");
        }

        return output.Trim();
    }

    [Fact]
    public async Task Process_PutThenSearch_FindsContent()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("OLLAMA_AVAILABLE"), "false", StringComparison.OrdinalIgnoreCase))
        {
            throw new SkipException("Skipping because OLLAMA_AVAILABLE=false (vector embeddings unavailable).");
        }

        // Act: Insert content
        var putOutput = await this.ExecuteKmAsync($"put \"ciao mondo\" --config {this._configPath}").ConfigureAwait(false);
        var putResult = JsonSerializer.Deserialize<JsonElement>(putOutput);
        var insertedId = putResult.GetProperty("id").GetString();
        Assert.NotNull(insertedId);
        Assert.True(putResult.GetProperty("completed").GetBoolean());

        // Act: Search for content
        var searchOutput = await this.ExecuteKmAsync($"search \"ciao\" --config {this._configPath} --format json").ConfigureAwait(false);
        var searchResult = JsonSerializer.Deserialize<JsonElement>(searchOutput);

        // Assert: Verify actual results
        Assert.Equal(1, searchResult.GetProperty("totalResults").GetInt32());
        var results = searchResult.GetProperty("results").EnumerateArray().ToArray();
        Assert.Single(results);
        Assert.Equal(insertedId, results[0].GetProperty("id").GetString());
        Assert.Contains("ciao", results[0].GetProperty("content").GetString()!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Process_BooleanAnd_FindsOnlyMatchingBoth()
    {
        // Arrange
        await this.ExecuteKmAsync($"put \"docker and kubernetes\" --config {this._configPath}").ConfigureAwait(false);
        await this.ExecuteKmAsync($"put \"only docker\" --config {this._configPath}").ConfigureAwait(false);

        // Act
        var output = await this.ExecuteKmAsync($"search \"docker AND kubernetes\" --config {this._configPath} --format json").ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<JsonElement>(output);

        // Assert
        Assert.Equal(1, result.GetProperty("totalResults").GetInt32());
        var content = result.GetProperty("results")[0].GetProperty("content").GetString()!;
        Assert.Contains("docker", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("kubernetes", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Process_FieldSpecificStemming_FindsVariations()
    {
        // Arrange
        var putOutput = await this.ExecuteKmAsync($"put \"summary findings\" --config {this._configPath}").ConfigureAwait(false);
        var putResult = JsonSerializer.Deserialize<JsonElement>(putOutput);
        var id = putResult.GetProperty("id").GetString();

        // Act: Search for plural form in content field
        var searchOutput = await this.ExecuteKmAsync($"search \"content:summaries\" --config {this._configPath} --format json").ConfigureAwait(false);
        var searchResult = JsonSerializer.Deserialize<JsonElement>(searchOutput);

        // Assert: Should find "summary" via stemming
        Assert.Equal(1, searchResult.GetProperty("totalResults").GetInt32());
        Assert.Equal(id, searchResult.GetProperty("results")[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task Process_MongoJsonQuery_FindsCorrectResults()
    {
        // Arrange
        var id1 = JsonSerializer.Deserialize<JsonElement>(
            await this.ExecuteKmAsync($"put \"kubernetes guide\" --config {this._configPath}").ConfigureAwait(false)
        ).GetProperty("id").GetString();

        await this.ExecuteKmAsync($"put \"docker guide\" --config {this._configPath}").ConfigureAwait(false);

        // Act: MongoDB JSON format - escape quotes for process arguments
        const string jsonQuery = "{\\\"content\\\": \\\"kubernetes\\\"}";
        var output = await this.ExecuteKmAsync($"search \"{jsonQuery}\" --config {this._configPath} --format json").ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<JsonElement>(output);

        // Assert
        Assert.Equal(1, result.GetProperty("totalResults").GetInt32());
        Assert.Equal(id1, result.GetProperty("results")[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task Process_DefaultMinRelevance_FindsResults()
    {
        // Regression test for BM25 normalization bug

        // Arrange
        await this.ExecuteKmAsync($"put \"test content\" --config {this._configPath}").ConfigureAwait(false);

        // Act: Don't specify min-relevance - use default 0.3
        var output = await this.ExecuteKmAsync($"search \"test\" --config {this._configPath} --format json").ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<JsonElement>(output);

        // Assert: Should find results despite default MinRelevance=0.3
        Assert.True(result.GetProperty("totalResults").GetInt32() > 0, "BM25 bug: default MinRelevance filters all results!");

        var relevance = result.GetProperty("results")[0].GetProperty("relevance").GetSingle();
        Assert.True(relevance >= 0.3f, $"Relevance {relevance} below 0.3 threshold");
    }

    [Fact]
    public async Task Process_ComplexNestedQuery_FindsCorrectMatches()
    {
        // Arrange
        var id1 = JsonSerializer.Deserialize<JsonElement>(
            await this.ExecuteKmAsync($"put \"docker kubernetes guide\" --config {this._configPath}").ConfigureAwait(false)
        ).GetProperty("id").GetString();

        var id2 = JsonSerializer.Deserialize<JsonElement>(
            await this.ExecuteKmAsync($"put \"docker helm charts\" --config {this._configPath}").ConfigureAwait(false)
        ).GetProperty("id").GetString();

        await this.ExecuteKmAsync($"put \"ansible automation\" --config {this._configPath}").ConfigureAwait(false);

        // Act: Nested query
        var output = await this.ExecuteKmAsync($"search \"docker AND (kubernetes OR helm)\" --config {this._configPath} --format json").ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<JsonElement>(output);

        // Assert
        Assert.Equal(2, result.GetProperty("totalResults").GetInt32());
        var ids = result.GetProperty("results").EnumerateArray()
            .Select(r => r.GetProperty("id").GetString())
            .ToHashSet();

        Assert.Contains(id1, ids);
        Assert.Contains(id2, ids);
    }
}
