// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Core.Config;
using KernelMemory.Core.Config.Cache;
using KernelMemory.Main.CLI.Commands;
using Spectre.Console.Cli;
using Xunit;

namespace KernelMemory.Main.Tests.Integration;

/// <summary>
/// Tests for ConfigCommand behavior.
/// These tests verify that config command shows the entire configuration,
/// not just a single node.
/// </summary>
public sealed class ConfigCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;

    public ConfigCommandTests()
    {
        this._tempDir = Path.Combine(Path.GetTempPath(), $"km-config-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(this._tempDir);

        this._configPath = Path.Combine(this._tempDir, "config.json");

        // Create test config with MULTIPLE nodes to verify entire config is shown
        var config = new AppConfig
        {
            Nodes = new Dictionary<string, NodeConfig>
            {
                ["personal"] = NodeConfig.CreateDefaultPersonalNode(Path.Combine(this._tempDir, "nodes", "personal")),
                ["work"] = NodeConfig.CreateDefaultPersonalNode(Path.Combine(this._tempDir, "nodes", "work")),
                ["shared"] = NodeConfig.CreateDefaultPersonalNode(Path.Combine(this._tempDir, "nodes", "shared"))
            },
            EmbeddingsCache = CacheConfig.CreateDefaultSqliteCache(Path.Combine(this._tempDir, "embeddings-cache.db")),
            LLMCache = null
        };

        var json = System.Text.Json.JsonSerializer.Serialize(config);
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
            // Ignore cleanup errors
        }
        catch (UnauthorizedAccessException)
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public void ConfigCommand_WithoutFlags_ShouldShowEntireConfiguration()
    {
        // This test verifies the bug: km config should show ALL nodes, not just the selected one

        // Arrange
        var config = ConfigParser.LoadFromFile(this._configPath);

        var settings = new ConfigCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json"  // Use JSON format for easier assertion
        };

        var command = new ConfigCommand(config);
        var context = new CommandContext(
            new[] { "--config", this._configPath },
            new EmptyRemainingArguments(),
            "config",
            null);

        // Capture stdout to verify output
        using var outputCapture = new StringWriter();
        var originalOutput = Console.Out;
        Console.SetOut(outputCapture);

        try
        {
            // Act
            var exitCode = command.ExecuteAsync(context, settings).GetAwaiter().GetResult();

            // Assert
            Assert.Equal(Constants.ExitCodeSuccess, exitCode);

            var output = outputCapture.ToString();

            // The output should contain ALL THREE nodes, not just one
            Assert.Contains("personal", output);
            Assert.Contains("work", output);
            Assert.Contains("shared", output);

            // Verify it's showing the entire config structure
            // Current bug: it only shows the first node's details
            // Expected: it should show all nodes
        }
        finally
        {
            Console.SetOut(originalOutput);
        }
    }

    [Fact]
    public void ConfigCommand_OutputStructure_ShouldMatchAppConfigFormat()
    {
        // This test verifies the BUG: km config output should match AppConfig structure
        // so users can copy/paste the output back into their config file

        // Arrange
        var config = ConfigParser.LoadFromFile(this._configPath);

        var settings = new ConfigCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json"
        };

        var command = new ConfigCommand(config);
        var context = new CommandContext(
            new[] { "--config", this._configPath },
            new EmptyRemainingArguments(),
            "config",
            null);

        // Capture stdout
        using var outputCapture = new StringWriter();
        var originalOutput = Console.Out;
        Console.SetOut(outputCapture);

        try
        {
            // Act
            var exitCode = command.ExecuteAsync(context, settings).GetAwaiter().GetResult();

            // Assert
            Assert.Equal(Constants.ExitCodeSuccess, exitCode);

            var output = outputCapture.ToString();
            var outputJson = System.Text.Json.JsonDocument.Parse(output);

            // BUG: Current output has "nodes" as an array
            // EXPECTED: "nodes" should be an object/dictionary (like in config file)
            var nodesElement = outputJson.RootElement.GetProperty("nodes");
            Assert.Equal(System.Text.Json.JsonValueKind.Object, nodesElement.ValueKind);
            // ^ This will FAIL with current code (it's an Array)

            // BUG: Current output has "embeddingsCache" nested under "cache"
            // EXPECTED: "embeddingsCache" should be at root level (like in config file)
            Assert.True(outputJson.RootElement.TryGetProperty("embeddingsCache", out _));
            // ^ This will FAIL with current code (no "embeddingsCache" at root)

            // Should NOT have a "cache" wrapper
            Assert.False(outputJson.RootElement.TryGetProperty("cache", out _));
            // ^ This will FAIL with current code (it has "cache" wrapper)
        }
        finally
        {
            Console.SetOut(originalOutput);
        }
    }

    [Fact]
    public void ConfigCommand_WithShowNodesFlag_ShouldShowAllNodesSummary()
    {
        // Arrange
        var config = ConfigParser.LoadFromFile(this._configPath);

        var settings = new ConfigCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            ShowNodes = true
        };

        var command = new ConfigCommand(config);
        var context = new CommandContext(
            new[] { "--config", this._configPath },
            new EmptyRemainingArguments(),
            "config",
            null);

        // Capture stdout
        using var outputCapture = new StringWriter();
        var originalOutput = Console.Out;
        Console.SetOut(outputCapture);

        try
        {
            // Act
            var exitCode = command.ExecuteAsync(context, settings).GetAwaiter().GetResult();

            // Assert
            Assert.Equal(Constants.ExitCodeSuccess, exitCode);

            var output = outputCapture.ToString();

            // Should show all three nodes
            Assert.Contains("personal", output);
            Assert.Contains("work", output);
            Assert.Contains("shared", output);
        }
        finally
        {
            Console.SetOut(originalOutput);
        }
    }

    /// <summary>
    /// Helper class to provide empty remaining arguments for CommandContext.
    /// </summary>
    private sealed class EmptyRemainingArguments : IRemainingArguments
    {
        public IReadOnlyList<string> Raw => Array.Empty<string>();
        public ILookup<string, string?> Parsed => Enumerable.Empty<string>().ToLookup(x => x, x => (string?)null);
    }
}
