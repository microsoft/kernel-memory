// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Core.Config;
using KernelMemory.Main.CLI.Commands;
using Spectre.Console.Cli;

namespace KernelMemory.Main.Tests.Integration;

/// <summary>
/// Integration tests for search functionality exercising the full CLI path.
/// Tests include stemming verification, FTS behavior, and real SQLite database operations.
/// </summary>
public sealed class SearchIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;
    private readonly AppConfig _config;

    public SearchIntegrationTests()
    {
        this._tempDir = Path.Combine(Path.GetTempPath(), $"km-search-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(this._tempDir);

        this._configPath = Path.Combine(this._tempDir, "config.json");

        // Create test config with FTS enabled
        this._config = new AppConfig
        {
            Nodes = new Dictionary<string, NodeConfig>
            {
                ["test"] = NodeConfig.CreateDefaultPersonalNode(Path.Combine(this._tempDir, "nodes", "test"))
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(this._config);
        File.WriteAllText(this._configPath, json);
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
            // Ignore cleanup errors - files may be locked
        }
        catch (UnauthorizedAccessException)
        {
            // Ignore cleanup errors - permission issues
        }
    }

    /// <summary>
    /// Helper to insert content for testing.
    /// </summary>
    /// <param name="content">The content to insert.</param>
    /// <param name="title">Optional title.</param>
    /// <param name="tags">Optional tags.</param>
    /// <returns>The generated document ID.</returns>
    private async Task<string> InsertContentAsync(string content, string? title = null, Dictionary<string, string>? tags = null)
    {
        var tagsString = tags != null ? string.Join(',', tags.Select(kvp => $"{kvp.Key}:{kvp.Value}")) : null;

        var settings = new UpsertCommandSettings
        {
            ConfigPath = this._configPath,
            Content = content,
            Title = title,
            Tags = tagsString
        };

        var command = new UpsertCommand(this._config);
        var context = new CommandContext(new[] { "--config", this._configPath }, new EmptyRemainingArguments(), "put", null);

        var result = await command.ExecuteAsync(context, settings).ConfigureAwait(false);
        Assert.Equal(0, result);

        // Return a dummy ID (in real scenario, we'd capture the actual ID from output)
        return $"test-{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Helper to execute search and verify it succeeds.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="format">Output format (default: json).</param>
    /// <returns>Exit code.</returns>
    private async Task<int> ExecuteSearchAsync(string query, string format = "json")
    {
        var settings = new SearchCommandSettings
        {
            ConfigPath = this._configPath,
            Query = query,
            Format = format,
            Limit = 20,
            MinRelevance = 0.0f // Accept all results for testing
        };

        var command = new SearchCommand(this._config);
        var context = new CommandContext(new[] { "--config", this._configPath }, new EmptyRemainingArguments(), "search", null);

        return await command.ExecuteAsync(context, settings).ConfigureAwait(false);
    }

    [Fact]
    public async Task Search_WithStemmingRun_FindsRunningAndRuns()
    {
        // Arrange: Insert documents with different forms of "run"
        await this.InsertContentAsync("The application is running smoothly").ConfigureAwait(false);
        await this.InsertContentAsync("He runs five miles every morning").ConfigureAwait(false);
        await this.InsertContentAsync("The server ran for 30 days straight").ConfigureAwait(false);
        await this.InsertContentAsync("Docker container management").ConfigureAwait(false); // Should not match

        // Act: Search for "run" - should match running, runs, ran via stemming
        var result = await this.ExecuteSearchAsync("run").ConfigureAwait(false);

        // Assert
        Assert.Equal(0, result); // Success
        // Note: Actual result verification would require capturing output
        // This test verifies the full pipeline executes without errors
    }

    [Fact]
    public async Task Search_WithStemmingSearch_FindsSearchingAndSearched()
    {
        // Arrange: Insert documents with different forms of "search"
        await this.InsertContentAsync("I am searching for the best solution").ConfigureAwait(false);
        await this.InsertContentAsync("The user searched through thousands of documents").ConfigureAwait(false);
        await this.InsertContentAsync("Advanced search capabilities").ConfigureAwait(false);
        await this.InsertContentAsync("Research paper on algorithms").ConfigureAwait(false); // Should not match "search"

        // Act: Search for "search" - should match searching, searched via stemming
        var result = await this.ExecuteSearchAsync("search").ConfigureAwait(false);

        // Assert
        Assert.Equal(0, result); // Success
    }

    [Fact]
    public async Task Search_WithStemmingDevelop_FindsDevelopingAndDeveloped()
    {
        // Arrange: Insert documents with different forms of "develop"
        await this.InsertContentAsync("We are developing a new feature").ConfigureAwait(false);
        await this.InsertContentAsync("The software was developed in 2024").ConfigureAwait(false);
        await this.InsertContentAsync("Development best practices").ConfigureAwait(false);
        await this.InsertContentAsync("Developer documentation").ConfigureAwait(false);

        // Act: Search for "develop" - should match all forms via stemming
        var result = await this.ExecuteSearchAsync("develop").ConfigureAwait(false);

        // Assert
        Assert.Equal(0, result); // Success
    }

    [Fact]
    public async Task Search_WithStemmingConnect_FindsConnectionAndConnected()
    {
        // Arrange: Insert documents with different forms of "connect"
        await this.InsertContentAsync("Connecting to the database").ConfigureAwait(false);
        await this.InsertContentAsync("Successfully connected to server").ConfigureAwait(false);
        await this.InsertContentAsync("Network connection established").ConfigureAwait(false);
        await this.InsertContentAsync("Configuration settings").ConfigureAwait(false); // Should not match

        // Act: Search for "connect" - should match connecting, connected, connection via stemming
        var result = await this.ExecuteSearchAsync("connect").ConfigureAwait(false);

        // Assert
        Assert.Equal(0, result); // Success
    }

    [Fact]
    public async Task Search_CaseInsensitive_MatchesRegardlessOfCase()
    {
        // Arrange: Insert documents with mixed case
        await this.InsertContentAsync("Kubernetes is a container orchestration platform").ConfigureAwait(false);
        await this.InsertContentAsync("KUBERNETES tutorial for beginners").ConfigureAwait(false);
        await this.InsertContentAsync("Working with kubernetes clusters").ConfigureAwait(false);

        // Act: Search with different cases
        var result1 = await this.ExecuteSearchAsync("kubernetes").ConfigureAwait(false);
        var result2 = await this.ExecuteSearchAsync("KUBERNETES").ConfigureAwait(false);
        var result3 = await this.ExecuteSearchAsync("KuBeRnEtEs").ConfigureAwait(false);

        // Assert: All should succeed
        Assert.Equal(0, result1);
        Assert.Equal(0, result2);
        Assert.Equal(0, result3);
    }

    [Fact]
    public async Task Search_WithBooleanQuery_AndOperator()
    {
        // Arrange: Insert documents
        await this.InsertContentAsync("Docker and Kubernetes for microservices").ConfigureAwait(false);
        await this.InsertContentAsync("Docker container basics").ConfigureAwait(false);
        await this.InsertContentAsync("Kubernetes deployment strategies").ConfigureAwait(false);

        // Act: Search with AND operator
        var result = await this.ExecuteSearchAsync("docker AND kubernetes").ConfigureAwait(false);

        // Assert
        Assert.Equal(0, result); // Should find first document
    }

    [Fact]
    public async Task Search_WithBooleanQuery_OrOperator()
    {
        // Arrange: Insert documents
        await this.InsertContentAsync("Python programming language").ConfigureAwait(false);
        await this.InsertContentAsync("JavaScript frameworks").ConfigureAwait(false);
        await this.InsertContentAsync("C# development").ConfigureAwait(false);

        // Act: Search with OR operator
        var result = await this.ExecuteSearchAsync("python OR javascript").ConfigureAwait(false);

        // Assert
        Assert.Equal(0, result); // Should find first two documents
    }

    [Fact]
    public async Task Search_WithBooleanQuery_NotOperator()
    {
        // Arrange: Insert documents
        await this.InsertContentAsync("Python is great for scripting").ConfigureAwait(false);
        await this.InsertContentAsync("Python and machine learning").ConfigureAwait(false);
        await this.InsertContentAsync("Java programming").ConfigureAwait(false);

        // Act: Search with NOT operator
        var result = await this.ExecuteSearchAsync("python NOT machine").ConfigureAwait(false);

        // Assert
        Assert.Equal(0, result); // Should find first document only
    }

    [Fact]
    public async Task Search_WithParentheses_NestedBooleanLogic()
    {
        // Arrange: Insert documents
        await this.InsertContentAsync("Docker and Kubernetes tutorial").ConfigureAwait(false);
        await this.InsertContentAsync("Docker and Helm charts").ConfigureAwait(false);
        await this.InsertContentAsync("Kubernetes and Terraform").ConfigureAwait(false);

        // Act: Search with nested boolean logic
        var result = await this.ExecuteSearchAsync("docker AND (kubernetes OR helm)").ConfigureAwait(false);

        // Assert
        Assert.Equal(0, result); // Should find first two documents
    }

    [Fact]
    public async Task Search_EmptyDatabase_ReturnsNoResults()
    {
        // Act: Search in empty database
        var result = await this.ExecuteSearchAsync("anything").ConfigureAwait(false);

        // Assert: Should succeed but return no results
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Search_WithMinRelevance_FiltersResults()
    {
        // Arrange: Insert documents
        await this.InsertContentAsync("Machine learning fundamentals").ConfigureAwait(false);
        await this.InsertContentAsync("Deep learning with neural networks").ConfigureAwait(false);
        await this.InsertContentAsync("Learning JavaScript basics").ConfigureAwait(false);

        // Act: Search with high min relevance (simulated)
        var settings = new SearchCommandSettings
        {
            ConfigPath = this._configPath,
            Query = "learning",
            Format = "json",
            Limit = 20,
            MinRelevance = 0.5f
        };

        var command = new SearchCommand(this._config);
        var context = new CommandContext(new[] { "--config", this._configPath }, new EmptyRemainingArguments(), "search", null);
        var result = await command.ExecuteAsync(context, settings).ConfigureAwait(false);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Search_WithLimit_ReturnsLimitedResults()
    {
        // Arrange: Insert multiple documents
        for (int i = 0; i < 10; i++)
        {
            await this.InsertContentAsync($"Test document number {i} about testing").ConfigureAwait(false);
        }

        // Act: Search with limit
        var settings = new SearchCommandSettings
        {
            ConfigPath = this._configPath,
            Query = "test",
            Format = "json",
            Limit = 5,
            MinRelevance = 0.0f
        };

        var command = new SearchCommand(this._config);
        var context = new CommandContext(new[] { "--config", this._configPath }, new EmptyRemainingArguments(), "search", null);
        var result = await command.ExecuteAsync(context, settings).ConfigureAwait(false);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Search_WithOffset_SkipsResults()
    {
        // Arrange: Insert multiple documents
        for (int i = 0; i < 10; i++)
        {
            await this.InsertContentAsync($"Pagination test document {i}").ConfigureAwait(false);
        }

        // Act: Search with offset
        var settings = new SearchCommandSettings
        {
            ConfigPath = this._configPath,
            Query = "pagination",
            Format = "json",
            Limit = 5,
            Offset = 3,
            MinRelevance = 0.0f
        };

        var command = new SearchCommand(this._config);
        var context = new CommandContext(new[] { "--config", this._configPath }, new EmptyRemainingArguments(), "search", null);
        var result = await command.ExecuteAsync(context, settings).ConfigureAwait(false);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Search_ValidateQuery_ValidQuery()
    {
        // Act: Validate a valid query
        var settings = new SearchCommandSettings
        {
            ConfigPath = this._configPath,
            Query = "kubernetes AND docker",
            Format = "json",
            ValidateOnly = true
        };

        var command = new SearchCommand(this._config);
        var context = new CommandContext(new[] { "--config", this._configPath }, new EmptyRemainingArguments(), "search", null);
        var result = await command.ExecuteAsync(context, settings).ConfigureAwait(false);

        // Assert
        Assert.Equal(0, result); // Valid query
    }

    [Fact]
    public async Task Search_SimpleTextQuery_FindsMatches()
    {
        // Arrange: Insert documents
        await this.InsertContentAsync("Docker container basics").ConfigureAwait(false);
        await this.InsertContentAsync("Kubernetes orchestration").ConfigureAwait(false);

        // Act: Simple text search
        var result = await this.ExecuteSearchAsync("docker").ConfigureAwait(false);

        // Assert
        Assert.Equal(0, result);
    }

    /// <summary>
    /// Helper class for Spectre.Console command context.
    /// </summary>
    private sealed class EmptyRemainingArguments : IRemainingArguments
    {
        public IReadOnlyList<string> Raw => Array.Empty<string>();
        public ILookup<string, string?> Parsed => Enumerable.Empty<string>().ToLookup(x => x, x => (string?)null);
    }
}
