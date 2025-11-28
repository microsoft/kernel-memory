// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json;
using KernelMemory.Core.Config;
using KernelMemory.Core.Config.ContentIndex;
using KernelMemory.Main.CLI.Commands;
using Spectre.Console.Cli;
using Xunit;

namespace KernelMemory.Main.Tests.Integration;

/// <summary>
/// Integration tests verifying that readonly commands do NOT create files or directories.
/// Bug A: "km list" was creating ~/.km/nodes/personal/content.db when it shouldn't.
/// </summary>
public sealed class ReadonlyCommandTests : IDisposable
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };
    private readonly string _tempDir;
    private readonly string _configPath;
    private readonly string _dbPath;

    public ReadonlyCommandTests()
    {
        // Create temp directory for test config (but NOT for database)
        this._tempDir = Path.Combine(Path.GetTempPath(), $"km-readonly-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(this._tempDir);

        // Database path is in a SUBDIRECTORY that doesn't exist yet
        // This allows us to test if commands create the directory
        this._dbPath = Path.Combine(this._tempDir, "nodes", "test-node", "test.db");
        this._configPath = Path.Combine(this._tempDir, "config.json");

        // Create test config pointing to non-existent database
        var config = new AppConfig
        {
            Nodes = new Dictionary<string, NodeConfig>
            {
                ["test-node"] = new NodeConfig
                {
                    Id = "test-node",
                    ContentIndex = new SqliteContentIndexConfig { Path = this._dbPath }
                }
            }
        };

        var json = JsonSerializer.Serialize(config, s_jsonOptions);
        File.WriteAllText(this._configPath, json);
    }

    public void Dispose()
    {
        if (Directory.Exists(this._tempDir))
        {
            Directory.Delete(this._tempDir, recursive: true);
        }
    }

    private static CommandContext CreateTestContext(string commandName)
    {
        return new CommandContext([], new EmptyRemainingArguments(), commandName, null);
    }

    [Fact]
    public async Task BugA_ListCommand_NonExistentDatabase_ShouldNotCreateDirectory()
    {
        // BUG A: Readonly operations like "km list" should NEVER create files/directories
        // Expected: Should fail with appropriate error if database doesn't exist
        // Actual: Creates the database directory and file

        // Arrange
        var config = ConfigParser.LoadFromFile(this._configPath);
        var dbDir = Path.GetDirectoryName(this._dbPath);
        Assert.NotNull(dbDir);

        // Verify database directory does not exist yet
        Assert.False(Directory.Exists(dbDir), "Database directory should not exist before test");
        Assert.False(File.Exists(this._dbPath), "Database file should not exist before test");

        var settings = new ListCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json"
        };

        var command = new ListCommand(config);
        var context = CreateTestContext("list");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings).ConfigureAwait(false);

        // Assert - With friendly first-run UX, missing DB returns success (0) not error
        // The key is that it should NOT create any files/directories
        Assert.Equal(Constants.ExitCodeSuccess, exitCode); // First-run is not an error
        Assert.False(Directory.Exists(dbDir),
            $"BUG: ListCommand (readonly) should NOT create directory: {dbDir}");
        Assert.False(File.Exists(this._dbPath),
            $"BUG: ListCommand (readonly) should NOT create database: {this._dbPath}");
    }

    [Fact]
    public async Task BugA_GetCommand_NonExistentDatabase_ShouldNotCreateDirectory()
    {
        // BUG A: Readonly operations like "km get" should NEVER create files/directories

        // Arrange
        var config = ConfigParser.LoadFromFile(this._configPath);
        var dbDir = Path.GetDirectoryName(this._dbPath);
        Assert.NotNull(dbDir);

        // Verify database directory does not exist yet
        Assert.False(Directory.Exists(dbDir), "Database directory should not exist before test");
        Assert.False(File.Exists(this._dbPath), "Database file should not exist before test");

        var settings = new GetCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Id = "test-id"
        };

        var command = new GetCommand(config);
        var context = CreateTestContext("get");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings).ConfigureAwait(false);

        // Assert - With friendly first-run UX, missing DB returns success (0) not error
        // The key is that it should NOT create any files/directories
        Assert.Equal(Constants.ExitCodeSuccess, exitCode); // First-run is not an error
        Assert.False(Directory.Exists(dbDir),
            $"BUG: GetCommand (readonly) should NOT create directory: {dbDir}");
        Assert.False(File.Exists(this._dbPath),
            $"BUG: GetCommand (readonly) should NOT create database: {this._dbPath}");
    }

    [Fact]
    public async Task BugA_NodesCommand_NonExistentDatabase_ShouldNotCreateDirectory()
    {
        // BUG A: Readonly operations like "km nodes" should NEVER create files/directories
        // NodesCommand doesn't even need the database - it just reads config

        // Arrange
        var config = ConfigParser.LoadFromFile(this._configPath);
        var dbDir = Path.GetDirectoryName(this._dbPath);
        Assert.NotNull(dbDir);

        // Verify database directory does not exist yet
        Assert.False(Directory.Exists(dbDir), "Database directory should not exist before test");

        var settings = new NodesCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json"
        };

        var command = new NodesCommand(config);
        var context = CreateTestContext("nodes");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings).ConfigureAwait(false);

        // Assert - This test SHOULD FAIL initially (reproducing the bug)
        // NodesCommand only reads config, shouldn't touch the database at all
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
        Assert.False(Directory.Exists(dbDir),
            $"BUG: NodesCommand (readonly) should NOT create directory: {dbDir}");
        Assert.False(File.Exists(this._dbPath),
            $"BUG: NodesCommand (readonly) should NOT create database: {this._dbPath}");
    }

    [Fact]
    public async Task BugA_ConfigCommand_NonExistentDatabase_ShouldNotCreateDirectory()
    {
        // BUG A: Readonly operations like "km config" should NEVER create files/directories
        // ConfigCommand doesn't even need the database - it just reads config

        // Arrange
        var config = ConfigParser.LoadFromFile(this._configPath);
        var dbDir = Path.GetDirectoryName(this._dbPath);
        Assert.NotNull(dbDir);

        // Verify database directory does not exist yet
        Assert.False(Directory.Exists(dbDir), "Database directory should not exist before test");

        var settings = new ConfigCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json"
        };

        var command = new ConfigCommand(config);
        var context = CreateTestContext("config");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings).ConfigureAwait(false);

        // Assert - This test SHOULD FAIL initially (reproducing the bug)
        // ConfigCommand only reads config, shouldn't touch the database at all
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
        Assert.False(Directory.Exists(dbDir),
            $"BUG: ConfigCommand (readonly) should NOT create directory: {dbDir}");
        Assert.False(File.Exists(this._dbPath),
            $"BUG: ConfigCommand (readonly) should NOT create database: {this._dbPath}");
    }

    /// <summary>
    /// Simple test implementation of IRemainingArguments.
    /// </summary>
    private sealed class EmptyRemainingArguments : IRemainingArguments
    {
        public IReadOnlyList<string> Raw => Array.Empty<string>();
        public ILookup<string, string?> Parsed => Enumerable.Empty<string>().ToLookup(x => x, x => (string?)null);
    }
}
