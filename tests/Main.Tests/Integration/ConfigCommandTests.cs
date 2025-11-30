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
/// <remarks>
/// These tests capture Console.Out, which is a global shared resource.
/// Running them in parallel with other tests that write to Console.Out
/// (like HumanOutputFormatterTests) causes output contamination.
/// The [Collection] attribute ensures these tests run serially.
/// </remarks>
[Collection("ConsoleOutputTests")]
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

        var configPathService = new KernelMemory.Main.CLI.Infrastructure.ConfigPathService(this._configPath);
        var command = new ConfigCommand(config, configPathService);
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

        var configPathService = new KernelMemory.Main.CLI.Infrastructure.ConfigPathService(this._configPath);
        var command = new ConfigCommand(config, configPathService);
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

        var configPathService = new KernelMemory.Main.CLI.Infrastructure.ConfigPathService(this._configPath);
        var command = new ConfigCommand(config, configPathService);
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

    [Fact]
    public void ConfigCommand_WithCreate_CreatesConfigFile()
    {
        // Arrange: Use a non-existent config path
        var newConfigPath = Path.Combine(this._tempDir, "new-config.json");
        Assert.False(File.Exists(newConfigPath));

        var config = ConfigParser.LoadFromFile(this._configPath);
        var configPathService = new KernelMemory.Main.CLI.Infrastructure.ConfigPathService(newConfigPath);
        var command = new ConfigCommand(config, configPathService);

        var settings = new ConfigCommandSettings
        {
            ConfigPath = newConfigPath,
            Create = true,
            Format = "json"
        };

        var context = new CommandContext(
            new[] { "--config", newConfigPath, "--create" },
            new EmptyRemainingArguments(),
            "config",
            null);

        // Act
        var exitCode = command.ExecuteAsync(context, settings).GetAwaiter().GetResult();

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
        Assert.True(File.Exists(newConfigPath), "Config file should be created");

        // Verify the file content is valid JSON
        var createdJson = File.ReadAllText(newConfigPath);
        var createdConfig = System.Text.Json.JsonDocument.Parse(createdJson);
        Assert.NotNull(createdConfig);
    }

    [Fact]
    public void ConfigCommand_WithCreate_WhenFileExists_ReturnsError()
    {
        // Arrange: Config file already exists (from constructor)
        Assert.True(File.Exists(this._configPath));

        var config = ConfigParser.LoadFromFile(this._configPath);
        var configPathService = new KernelMemory.Main.CLI.Infrastructure.ConfigPathService(this._configPath);
        var command = new ConfigCommand(config, configPathService);

        var settings = new ConfigCommandSettings
        {
            ConfigPath = this._configPath,
            Create = true,
            Format = "json"
        };

        var context = new CommandContext(
            new[] { "--config", this._configPath, "--create" },
            new EmptyRemainingArguments(),
            "config",
            null);

        // Suppress console output
        using var outputCapture = new StringWriter();
        using var errorCapture = new StringWriter();
        var originalOutput = Console.Out;
        var originalError = Console.Error;
        Console.SetOut(outputCapture);
        Console.SetError(errorCapture);

        try
        {
            // Act
            var exitCode = command.ExecuteAsync(context, settings).GetAwaiter().GetResult();

            // Assert
            Assert.Equal(Constants.ExitCodeUserError, exitCode);

            // Error message goes to Console.Error
            var errorOutput = errorCapture.ToString();
            Assert.Contains("already exists", errorOutput);
        }
        finally
        {
            Console.SetOut(originalOutput);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void ConfigCommand_WithoutConfigFile_StillSucceeds()
    {
        // Arrange: Use a non-existent config path (default config will be used)
        var missingConfigPath = Path.Combine(this._tempDir, "missing-config.json");
        Assert.False(File.Exists(missingConfigPath));

        var config = AppConfig.CreateDefault();
        var configPathService = new KernelMemory.Main.CLI.Infrastructure.ConfigPathService(missingConfigPath);
        var command = new ConfigCommand(config, configPathService);

        var settings = new ConfigCommandSettings
        {
            ConfigPath = missingConfigPath,
            Format = "json",
            NoColor = false  // Enable colors
        };

        var context = new CommandContext(
            new[] { "--config", missingConfigPath },
            new EmptyRemainingArguments(),
            "config",
            null);

        // Suppress console output (warning goes to AnsiConsole which is hard to capture in tests)
        using var outputCapture = new StringWriter();
        var originalOutput = Console.Out;
        Console.SetOut(outputCapture);

        try
        {
            // Act
            var exitCode = command.ExecuteAsync(context, settings).GetAwaiter().GetResult();

            // Assert
            // The key behavior: command succeeds even without config file
            Assert.Equal(Constants.ExitCodeSuccess, exitCode);

            // Should still output valid JSON config
            var output = outputCapture.ToString();
            var json = System.Text.Json.JsonDocument.Parse(output);
            Assert.NotNull(json);
            Assert.True(json.RootElement.TryGetProperty("nodes", out _));
        }
        finally
        {
            Console.SetOut(originalOutput);
        }
    }

    [Fact]
    public void ConfigCommand_OutputJson_DoesNotContainNullFields()
    {
        // Arrange
        var config = ConfigParser.LoadFromFile(this._configPath);
        var configPathService = new KernelMemory.Main.CLI.Infrastructure.ConfigPathService(this._configPath);
        var command = new ConfigCommand(config, configPathService);

        var settings = new ConfigCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json"
        };

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

            // Verify no null fields are serialized
            Assert.DoesNotContain("\"connectionString\": null", output);
            Assert.DoesNotContain("\"embeddings\": null", output);
            Assert.DoesNotContain("\"fileStorage\": null", output);
            Assert.DoesNotContain("\"repoStorage\": null", output);
            Assert.DoesNotContain("\"llmCache\": null", output);
        }
        finally
        {
            Console.SetOut(originalOutput);
        }
    }

    [Fact]
    public void ConfigCommand_OutputJson_ContainsCorrectDiscriminators()
    {
        // Arrange
        var config = ConfigParser.LoadFromFile(this._configPath);
        var configPathService = new KernelMemory.Main.CLI.Infrastructure.ConfigPathService(this._configPath);
        var command = new ConfigCommand(config, configPathService);

        var settings = new ConfigCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json"
        };

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

            // Verify content index uses "sqlite" (not a generic name)
            var contentIndex = outputJson.RootElement.GetProperty("nodes").GetProperty("personal").GetProperty("contentIndex");
            Assert.Equal("sqlite", contentIndex.GetProperty("type").GetString());

            // Verify search index uses "sqliteFTS" (not generic "fts")
            var searchIndex = outputJson.RootElement.GetProperty("nodes").GetProperty("personal").GetProperty("searchIndexes")[0];
            Assert.Equal("sqliteFTS", searchIndex.GetProperty("type").GetString());

            // Verify cache uses "Sqlite"
            var embeddingsCache = outputJson.RootElement.GetProperty("embeddingsCache");
            Assert.Equal("Sqlite", embeddingsCache.GetProperty("type").GetString());
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
