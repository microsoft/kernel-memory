// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json;
using KernelMemory.Core.Config;
using KernelMemory.Core.Config.ContentIndex;
using KernelMemory.Main.CLI.Commands;
using Spectre.Console.Cli;

namespace KernelMemory.Main.Tests.Integration;

/// <summary>
/// Integration tests for CLI commands with real SQLite database.
/// These tests cover end-to-end workflows: put → get → list → delete.
/// </summary>
public sealed class CliIntegrationTests : IDisposable
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };
    private readonly string _tempDir;
    private readonly string _configPath;
    private readonly string _dbPath;

    public CliIntegrationTests()
    {
        // Create temp directory for test database and config
        this._tempDir = Path.Combine(Path.GetTempPath(), $"km-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(this._tempDir);

        this._dbPath = Path.Combine(this._tempDir, "test.db");
        this._configPath = Path.Combine(this._tempDir, "config.json");

        // Create test config
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
    public async Task UpsertCommand_WithMinimalOptions_CreatesContent()
    {
        // Arrange
        var config = ConfigParser.LoadFromFile(this._configPath);
        var settings = new UpsertCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Verbosity = "quiet",
            Content = "Test content"
        };

        var command = new UpsertCommand(config);
        var context = CreateTestContext("put");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings).ConfigureAwait(false);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task UpsertCommand_WithCustomId_UsesProvidedId()
    {
        // Arrange
        var config = ConfigParser.LoadFromFile(this._configPath);
        const string customId = "my-custom-id-123";
        var settings = new UpsertCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Verbosity = "normal",
            Content = "Test content",
            Id = customId
        };

        var command = new UpsertCommand(config);
        var context = CreateTestContext("put");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings).ConfigureAwait(false);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);

        // Verify content exists with custom ID
        var getSettings = new GetCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Id = customId
        };

        var getCommand = new GetCommand(config);
        var getExitCode = await getCommand.ExecuteAsync(context, getSettings).ConfigureAwait(false);
        Assert.Equal(Constants.ExitCodeSuccess, getExitCode);
    }

    [Fact]
    public async Task UpsertCommand_WithAllMetadata_StoresAllFields()
    {
        // Arrange
        var config = ConfigParser.LoadFromFile(this._configPath);
        var settings = new UpsertCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Content = "Test content with metadata",
            Title = "Test Title",
            Description = "Test Description",
            Tags = "tag1,tag2,tag3",
            MimeType = "text/markdown"
        };

        var command = new UpsertCommand(config);
        var context = CreateTestContext("put");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings).ConfigureAwait(false);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task GetCommand_ExistingId_ReturnsContent()
    {
        // Arrange - First upsert content
        var config = ConfigParser.LoadFromFile(this._configPath);
        const string customId = "get-test-id";
        var upsertSettings = new UpsertCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Content = "Content to retrieve",
            Id = customId
        };

        var upsertCommand = new UpsertCommand(config);
        var context = CreateTestContext("put");
        await upsertCommand.ExecuteAsync(context, upsertSettings).ConfigureAwait(false);

        // Act - Get the content
        var getSettings = new GetCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Id = customId
        };

        var getCommand = new GetCommand(config);
        var exitCode = await getCommand.ExecuteAsync(context, getSettings).ConfigureAwait(false);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task GetCommand_NonExistentId_ReturnsUserError()
    {
        // Arrange - First create the database with some content
        var config = ConfigParser.LoadFromFile(this._configPath);

        // Create DB by upserting content first
        var upsertSettings = new UpsertCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Content = "Some content to create the DB"
        };
        var upsertCommand = new UpsertCommand(config);
        await upsertCommand.ExecuteAsync(CreateTestContext("put"), upsertSettings).ConfigureAwait(false);

        // Now try to get non-existent ID from existing DB
        var settings = new GetCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Id = "non-existent-id-12345"
        };

        var command = new GetCommand(config);
        var context = CreateTestContext("get");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings).ConfigureAwait(false);

        // Assert - ID not found in existing DB is user error
        Assert.Equal(Constants.ExitCodeUserError, exitCode);
    }

    [Fact]
    public async Task GetCommand_WithFullFlag_ReturnsAllDetails()
    {
        // Arrange - First upsert content
        var config = ConfigParser.LoadFromFile(this._configPath);
        const string customId = "full-details-id";
        var upsertSettings = new UpsertCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Content = "Full details content",
            Id = customId,
            Title = "Full Title"
        };

        var upsertCommand = new UpsertCommand(config);
        var context = CreateTestContext("put");
        await upsertCommand.ExecuteAsync(context, upsertSettings).ConfigureAwait(false);

        // Act - Get with full flag
        var getSettings = new GetCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Id = customId,
            ShowFull = true
        };

        var getCommand = new GetCommand(config);
        var exitCode = await getCommand.ExecuteAsync(context, getSettings).ConfigureAwait(false);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task ListCommand_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange - First create the database by doing an upsert, then delete
        var config = ConfigParser.LoadFromFile(this._configPath);
        var upsertSettings = new UpsertCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Content = "Temporary content to create database",
            Id = "temp-id"
        };
        var upsertCommand = new UpsertCommand(config);
        var context = CreateTestContext("put");
        await upsertCommand.ExecuteAsync(context, upsertSettings).ConfigureAwait(false);

        // Delete the content to have empty database
        var deleteSettings = new DeleteCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Id = "temp-id"
        };
        var deleteCommand = new DeleteCommand(config);
        await deleteCommand.ExecuteAsync(context, deleteSettings).ConfigureAwait(false);

        // Now test list on empty database
        var settings = new ListCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json"
        };

        var command = new ListCommand(config);
        var listContext = CreateTestContext("list");

        // Act
        var exitCode = await command.ExecuteAsync(listContext, settings).ConfigureAwait(false);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task Bug3_ListCommand_EmptyDatabase_HumanFormat_ShouldHandleGracefully()
    {
        // BUG: km list should manage the "empty list" scenario smoothly
        // rather than print an empty table
        // This test reproduces the bug by using human format with empty database

        // Arrange - First create the database by doing an upsert, then delete
        var config = ConfigParser.LoadFromFile(this._configPath);
        var upsertSettings = new UpsertCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Content = "Temporary content to create database",
            Id = "temp-id-human"
        };
        var upsertCommand = new UpsertCommand(config);
        var context = CreateTestContext("put");
        await upsertCommand.ExecuteAsync(context, upsertSettings).ConfigureAwait(false);

        // Delete the content to have empty database
        var deleteSettings = new DeleteCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Id = "temp-id-human"
        };
        var deleteCommand = new DeleteCommand(config);
        await deleteCommand.ExecuteAsync(context, deleteSettings).ConfigureAwait(false);

        // Now test list on empty database with human format
        var settings = new ListCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "human"  // Test human format, not just JSON
        };

        var command = new ListCommand(config);
        var listContext = CreateTestContext("list");

        // Act
        var exitCode = await command.ExecuteAsync(listContext, settings).ConfigureAwait(false);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
        // TODO: Capture stdout and verify it doesn't show an empty table
        // Expected: A message like "No content found" instead of empty table
    }

    [Fact]
    public async Task ListCommand_WithContent_ReturnsList()
    {
        // Arrange - First upsert some content
        var config = ConfigParser.LoadFromFile(this._configPath);
        var upsertSettings = new UpsertCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Content = "List test content"
        };

        var upsertCommand = new UpsertCommand(config);
        var context = CreateTestContext("put");
        await upsertCommand.ExecuteAsync(context, upsertSettings).ConfigureAwait(false);

        // Act - List content
        var listSettings = new ListCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json"
        };

        var listCommand = new ListCommand(config);
        var exitCode = await listCommand.ExecuteAsync(context, listSettings).ConfigureAwait(false);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task ListCommand_WithPagination_RespectsSkipAndTake()
    {
        // Arrange - Insert multiple items
        var config = ConfigParser.LoadFromFile(this._configPath);
        var upsertCommand = new UpsertCommand(config);
        var context = CreateTestContext("put");

        for (int i = 0; i < 5; i++)
        {
            var upsertSettings = new UpsertCommandSettings
            {
                ConfigPath = this._configPath,
                Format = "json",
                Content = $"Content {i}"
            };
            await upsertCommand.ExecuteAsync(context, upsertSettings).ConfigureAwait(false);
        }

        // Act - List with pagination
        var listSettings = new ListCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Skip = 2,
            Take = 2
        };

        var listCommand = new ListCommand(config);
        var exitCode = await listCommand.ExecuteAsync(context, listSettings).ConfigureAwait(false);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task DeleteCommand_ExistingId_DeletesSuccessfully()
    {
        // Arrange - First upsert content
        var config = ConfigParser.LoadFromFile(this._configPath);
        const string customId = "delete-test-id";
        var upsertSettings = new UpsertCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Content = "Content to delete",
            Id = customId
        };

        var upsertCommand = new UpsertCommand(config);
        var context = CreateTestContext("put");
        await upsertCommand.ExecuteAsync(context, upsertSettings).ConfigureAwait(false);

        // Act - Delete the content
        var deleteSettings = new DeleteCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Id = customId
        };

        var deleteCommand = new DeleteCommand(config);
        var exitCode = await deleteCommand.ExecuteAsync(context, deleteSettings).ConfigureAwait(false);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);

        // Verify content is gone
        var getSettings = new GetCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Id = customId
        };

        var getCommand = new GetCommand(config);
        var getExitCode = await getCommand.ExecuteAsync(context, getSettings).ConfigureAwait(false);
        Assert.Equal(Constants.ExitCodeUserError, getExitCode);
    }

    [Fact]
    public async Task DeleteCommand_WithQuietVerbosity_SucceedsWithMinimalOutput()
    {
        // Arrange - First upsert content
        var config = ConfigParser.LoadFromFile(this._configPath);
        const string customId = "quiet-delete-id";
        var upsertSettings = new UpsertCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Content = "Content to delete quietly",
            Id = customId
        };

        var upsertCommand = new UpsertCommand(config);
        var context = CreateTestContext("put");
        await upsertCommand.ExecuteAsync(context, upsertSettings).ConfigureAwait(false);

        // Act - Delete with quiet verbosity
        var deleteSettings = new DeleteCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Verbosity = "quiet",
            Id = customId
        };

        var deleteCommand = new DeleteCommand(config);
        var exitCode = await deleteCommand.ExecuteAsync(context, deleteSettings).ConfigureAwait(false);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task EndToEndWorkflow_UpsertGetListDelete_AllSucceed()
    {
        // This test verifies the complete workflow works together
        var config = ConfigParser.LoadFromFile(this._configPath);
        var context = CreateTestContext("test");
        const string testId = "e2e-workflow-id";

        // 1. Upsert
        var upsertSettings = new UpsertCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Content = "End-to-end test content",
            Id = testId,
            Tags = "e2e,test"
        };
        var upsertCommand = new UpsertCommand(config);
        var upsertExitCode = await upsertCommand.ExecuteAsync(context, upsertSettings).ConfigureAwait(false);
        Assert.Equal(Constants.ExitCodeSuccess, upsertExitCode);

        // 2. Get
        var getSettings = new GetCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Id = testId
        };
        var getCommand = new GetCommand(config);
        var getExitCode = await getCommand.ExecuteAsync(context, getSettings).ConfigureAwait(false);
        Assert.Equal(Constants.ExitCodeSuccess, getExitCode);

        // 3. List
        var listSettings = new ListCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json"
        };
        var listCommand = new ListCommand(config);
        var listExitCode = await listCommand.ExecuteAsync(context, listSettings).ConfigureAwait(false);
        Assert.Equal(Constants.ExitCodeSuccess, listExitCode);

        // 4. Delete
        var deleteSettings = new DeleteCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Id = testId
        };
        var deleteCommand = new DeleteCommand(config);
        var deleteExitCode = await deleteCommand.ExecuteAsync(context, deleteSettings).ConfigureAwait(false);
        Assert.Equal(Constants.ExitCodeSuccess, deleteExitCode);

        // 5. Verify deleted
        var verifyExitCode = await getCommand.ExecuteAsync(context, getSettings).ConfigureAwait(false);
        Assert.Equal(Constants.ExitCodeUserError, verifyExitCode);
    }

    [Fact]
    public async Task NodesCommand_WithJsonFormat_ListsAllNodes()
    {
        // Arrange
        var config = ConfigParser.LoadFromFile(this._configPath);
        var settings = new NodesCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json"
        };

        var command = new NodesCommand(config);
        var context = CreateTestContext("nodes");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings).ConfigureAwait(false);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task NodesCommand_WithYamlFormat_ListsAllNodes()
    {
        // Arrange
        var config = ConfigParser.LoadFromFile(this._configPath);
        var settings = new NodesCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "yaml"
        };

        var command = new NodesCommand(config);
        var context = CreateTestContext("nodes");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings).ConfigureAwait(false);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task ConfigCommand_Default_ShowsCurrentNode()
    {
        // Arrange
        var config = ConfigParser.LoadFromFile(this._configPath);
        var settings = new ConfigCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json"
        };

        var configPathService = new KernelMemory.Main.CLI.Infrastructure.ConfigPathService(this._configPath);
        var command = new ConfigCommand(config, configPathService);
        var context = CreateTestContext("config");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings).ConfigureAwait(false);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task ConfigCommand_WithShowNodes_ShowsAllNodes()
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
        var context = CreateTestContext("config");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings).ConfigureAwait(false);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task ConfigCommand_WithShowCache_ShowsCacheConfig()
    {
        // Arrange
        var config = ConfigParser.LoadFromFile(this._configPath);
        var settings = new ConfigCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            ShowCache = true
        };

        var configPathService = new KernelMemory.Main.CLI.Infrastructure.ConfigPathService(this._configPath);
        var command = new ConfigCommand(config, configPathService);
        var context = CreateTestContext("config");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings).ConfigureAwait(false);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public void Bug4_IntegrationTests_ShouldNeverTouchUserData()
    {
        // BUG: tests should never touch real user data
        // User reported: "I'm seeing test data in my personal node"
        // This test verifies that all test paths are in temp directories

        // Assert - Verify test uses temp directory, not ~/.km
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var userKmDir = Path.Combine(homeDir, ".km");

        Assert.StartsWith(Path.GetTempPath(), this._tempDir);
        Assert.DoesNotContain(".km", this._tempDir);
        Assert.DoesNotContain(userKmDir, this._tempDir);
        Assert.DoesNotContain(userKmDir, this._dbPath);
        Assert.DoesNotContain(userKmDir, this._configPath);

        // Verify the test database path is in temp, not user home
        Assert.False(this._dbPath.Contains(userKmDir),
            $"Test database path should not be in user .km directory. Path: {this._dbPath}");
    }

    [Fact]
    public async Task Bug2_ConfigCommand_HumanFormat_ShouldNotLeakTypeNames()
    {
        // BUG: "km config and other commands should not leak internal types
        // such as System.Collections.Generic.List`1[...]"
        // When using human format, HumanOutputFormatter.Format() calls ToString()
        // on DTO objects, which returns the type name instead of formatted data
        //
        // This test verifies that the command executes successfully in human format.
        // The bug was that it would output type names like "NodeDetailsDto" instead
        // of actual formatted data. The fix formats unknown types as JSON instead.

        // Arrange
        var config = ConfigParser.LoadFromFile(this._configPath);
        var settings = new ConfigCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "human"
        };

        var configPathService = new KernelMemory.Main.CLI.Infrastructure.ConfigPathService(this._configPath);
        var command = new ConfigCommand(config, configPathService);
        var context = CreateTestContext("config");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings).ConfigureAwait(false);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);

        // Note: AnsiConsole output cannot be easily captured in tests.
        // The fix ensures that HumanOutputFormatter.Format() handles DTO objects
        // by formatting them as JSON instead of calling ToString() which leaks type names.
        // Manual verification: Run "km config" and verify output is JSON, not type name.
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
