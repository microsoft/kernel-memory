// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json;
using KernelMemory.Core.Config;
using KernelMemory.Core.Config.ContentIndex;
using KernelMemory.Main.CLI.Commands;
using Spectre.Console.Cli;
using Xunit;

namespace KernelMemory.Main.Tests.Integration;

/// <summary>
/// Integration tests for CLI commands with real SQLite database.
/// These tests cover end-to-end workflows: upsert → get → list → delete.
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
        var settings = new UpsertCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Verbosity = "quiet",
            Content = "Test content"
        };

        var command = new UpsertCommand();
        var context = CreateTestContext("upsert");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task UpsertCommand_WithCustomId_UsesProvidedId()
    {
        // Arrange
        const string customId = "my-custom-id-123";
        var settings = new UpsertCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Verbosity = "normal",
            Content = "Test content",
            Id = customId
        };

        var command = new UpsertCommand();
        var context = CreateTestContext("upsert");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);

        // Verify content exists with custom ID
        var getSettings = new GetCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Id = customId
        };

        var getCommand = new GetCommand();
        var getExitCode = await getCommand.ExecuteAsync(context, getSettings);
        Assert.Equal(Constants.ExitCodeSuccess, getExitCode);
    }

    [Fact]
    public async Task UpsertCommand_WithAllMetadata_StoresAllFields()
    {
        // Arrange
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

        var command = new UpsertCommand();
        var context = CreateTestContext("upsert");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task GetCommand_ExistingId_ReturnsContent()
    {
        // Arrange - First upsert content
        const string customId = "get-test-id";
        var upsertSettings = new UpsertCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Content = "Content to retrieve",
            Id = customId
        };

        var upsertCommand = new UpsertCommand();
        var context = CreateTestContext("upsert");
        await upsertCommand.ExecuteAsync(context, upsertSettings);

        // Act - Get the content
        var getSettings = new GetCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Id = customId
        };

        var getCommand = new GetCommand();
        var exitCode = await getCommand.ExecuteAsync(context, getSettings);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task GetCommand_NonExistentId_ReturnsUserError()
    {
        // Arrange
        var settings = new GetCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Id = "non-existent-id-12345"
        };

        var command = new GetCommand();
        var context = CreateTestContext("get");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings);

        // Assert
        Assert.Equal(Constants.ExitCodeUserError, exitCode);
    }

    [Fact]
    public async Task GetCommand_WithFullFlag_ReturnsAllDetails()
    {
        // Arrange - First upsert content
        const string customId = "full-details-id";
        var upsertSettings = new UpsertCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Content = "Full details content",
            Id = customId,
            Title = "Full Title"
        };

        var upsertCommand = new UpsertCommand();
        var context = CreateTestContext("upsert");
        await upsertCommand.ExecuteAsync(context, upsertSettings);

        // Act - Get with full flag
        var getSettings = new GetCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Id = customId,
            ShowFull = true
        };

        var getCommand = new GetCommand();
        var exitCode = await getCommand.ExecuteAsync(context, getSettings);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task ListCommand_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var settings = new ListCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json"
        };

        var command = new ListCommand();
        var context = CreateTestContext("list");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task ListCommand_WithContent_ReturnsList()
    {
        // Arrange - First upsert some content
        var upsertSettings = new UpsertCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Content = "List test content"
        };

        var upsertCommand = new UpsertCommand();
        var context = CreateTestContext("upsert");
        await upsertCommand.ExecuteAsync(context, upsertSettings);

        // Act - List content
        var listSettings = new ListCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json"
        };

        var listCommand = new ListCommand();
        var exitCode = await listCommand.ExecuteAsync(context, listSettings);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task ListCommand_WithPagination_RespectsSkipAndTake()
    {
        // Arrange - Insert multiple items
        var upsertCommand = new UpsertCommand();
        var context = CreateTestContext("upsert");

        for (int i = 0; i < 5; i++)
        {
            var upsertSettings = new UpsertCommandSettings
            {
                ConfigPath = this._configPath,
                Format = "json",
                Content = $"Content {i}"
            };
            await upsertCommand.ExecuteAsync(context, upsertSettings);
        }

        // Act - List with pagination
        var listSettings = new ListCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Skip = 2,
            Take = 2
        };

        var listCommand = new ListCommand();
        var exitCode = await listCommand.ExecuteAsync(context, listSettings);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task DeleteCommand_ExistingId_DeletesSuccessfully()
    {
        // Arrange - First upsert content
        const string customId = "delete-test-id";
        var upsertSettings = new UpsertCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Content = "Content to delete",
            Id = customId
        };

        var upsertCommand = new UpsertCommand();
        var context = CreateTestContext("upsert");
        await upsertCommand.ExecuteAsync(context, upsertSettings);

        // Act - Delete the content
        var deleteSettings = new DeleteCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Id = customId
        };

        var deleteCommand = new DeleteCommand();
        var exitCode = await deleteCommand.ExecuteAsync(context, deleteSettings);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);

        // Verify content is gone
        var getSettings = new GetCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Id = customId
        };

        var getCommand = new GetCommand();
        var getExitCode = await getCommand.ExecuteAsync(context, getSettings);
        Assert.Equal(Constants.ExitCodeUserError, getExitCode);
    }

    [Fact]
    public async Task DeleteCommand_WithQuietVerbosity_SucceedsWithMinimalOutput()
    {
        // Arrange - First upsert content
        const string customId = "quiet-delete-id";
        var upsertSettings = new UpsertCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Content = "Content to delete quietly",
            Id = customId
        };

        var upsertCommand = new UpsertCommand();
        var context = CreateTestContext("upsert");
        await upsertCommand.ExecuteAsync(context, upsertSettings);

        // Act - Delete with quiet verbosity
        var deleteSettings = new DeleteCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Verbosity = "quiet",
            Id = customId
        };

        var deleteCommand = new DeleteCommand();
        var exitCode = await deleteCommand.ExecuteAsync(context, deleteSettings);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task EndToEndWorkflow_UpsertGetListDelete_AllSucceed()
    {
        // This test verifies the complete workflow works together
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
        var upsertCommand = new UpsertCommand();
        var upsertExitCode = await upsertCommand.ExecuteAsync(context, upsertSettings);
        Assert.Equal(Constants.ExitCodeSuccess, upsertExitCode);

        // 2. Get
        var getSettings = new GetCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Id = testId
        };
        var getCommand = new GetCommand();
        var getExitCode = await getCommand.ExecuteAsync(context, getSettings);
        Assert.Equal(Constants.ExitCodeSuccess, getExitCode);

        // 3. List
        var listSettings = new ListCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json"
        };
        var listCommand = new ListCommand();
        var listExitCode = await listCommand.ExecuteAsync(context, listSettings);
        Assert.Equal(Constants.ExitCodeSuccess, listExitCode);

        // 4. Delete
        var deleteSettings = new DeleteCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Id = testId
        };
        var deleteCommand = new DeleteCommand();
        var deleteExitCode = await deleteCommand.ExecuteAsync(context, deleteSettings);
        Assert.Equal(Constants.ExitCodeSuccess, deleteExitCode);

        // 5. Verify deleted
        var verifyExitCode = await getCommand.ExecuteAsync(context, getSettings);
        Assert.Equal(Constants.ExitCodeUserError, verifyExitCode);
    }

    [Fact]
    public async Task NodesCommand_WithJsonFormat_ListsAllNodes()
    {
        // Arrange
        var settings = new NodesCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json"
        };

        var command = new NodesCommand();
        var context = CreateTestContext("nodes");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task NodesCommand_WithYamlFormat_ListsAllNodes()
    {
        // Arrange
        var settings = new NodesCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "yaml"
        };

        var command = new NodesCommand();
        var context = CreateTestContext("nodes");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task ConfigCommand_Default_ShowsCurrentNode()
    {
        // Arrange
        var settings = new ConfigCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json"
        };

        var command = new ConfigCommand();
        var context = CreateTestContext("config");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task ConfigCommand_WithShowNodes_ShowsAllNodes()
    {
        // Arrange
        var settings = new ConfigCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            ShowNodes = true
        };

        var command = new ConfigCommand();
        var context = CreateTestContext("config");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
    }

    [Fact]
    public async Task ConfigCommand_WithShowCache_ShowsCacheConfig()
    {
        // Arrange
        var settings = new ConfigCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            ShowCache = true
        };

        var command = new ConfigCommand();
        var context = CreateTestContext("config");

        // Act
        var exitCode = await command.ExecuteAsync(context, settings);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);
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
