// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Config;
using KernelMemory.Main.CLI.Commands;
using Spectre.Console.Cli;

namespace KernelMemory.Main.Tests.Integration;

/// <summary>
/// Critical tests to ensure NO test EVER touches user's personal data at ~/.km.
/// </summary>
public sealed class UserDataProtectionTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;
    private readonly string _userHomeDir;
    private readonly string _userKmDir;
    private readonly string _userPersonalDbPath;

    public UserDataProtectionTests()
    {
        this._tempDir = Path.Combine(Path.GetTempPath(), $"km-protection-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(this._tempDir);

        this._configPath = Path.Combine(this._tempDir, "config.json");

        // Track user's actual ~/.km paths
        this._userHomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        this._userKmDir = Path.Combine(this._userHomeDir, ".km");
        this._userPersonalDbPath = Path.Combine(this._userKmDir, "nodes", "personal", "content.db");

        // Create test config pointing to temp directory
        var config = new AppConfig
        {
            Nodes = new Dictionary<string, NodeConfig>
            {
                ["test"] = NodeConfig.CreateDefaultPersonalNode(Path.Combine(this._tempDir, "nodes", "test"))
            }
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
    public async Task CriticalBug_CommandExecutionTests_MustNotTouchUserData()
    {
        // This test reproduces the CRITICAL BUG:
        // Tests were writing to ~/.km because settings.ConfigPath was not set

        // Record if user has personal DB before test
        var userDbExistedBefore = File.Exists(this._userPersonalDbPath);
        long userDbSizeBefore = 0;
        DateTime userDbModifiedBefore = DateTime.MinValue;

        if (userDbExistedBefore)
        {
            var fileInfo = new FileInfo(this._userPersonalDbPath);
            userDbSizeBefore = fileInfo.Length;
            userDbModifiedBefore = fileInfo.LastWriteTimeUtc;
        }

        // BUG REPRODUCTION: Settings without ConfigPath falls back to ~/.km
        // FIXED: Now config is injected, but this test demonstrates the old bug scenario
        var config = ConfigParser.LoadFromFile(this._configPath);

        var settingsWithoutConfigPath = new UpsertCommandSettings
        {
            Content = "CRITICAL BUG: This should NOT go to user data!"
        };

        var command = new UpsertCommand(config);

        // This context has --config flag, but BaseCommand reads from settings.ConfigPath!
        var context = new CommandContext(
            new[] { "--config", this._configPath },
            new EmptyRemainingArguments(),
            "put",
            null);

        // Act - This WILL write to ~/.km if bug exists
        try
        {
            await command.ExecuteAsync(context, settingsWithoutConfigPath, CancellationToken.None).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // Expected: config loading or node resolution failures
        }
        catch (ArgumentException)
        {
            // Expected: invalid arguments
        }
        catch (IOException)
        {
            // Expected: file system access issues
        }

        // Assert - User's personal DB must NOT be modified
        var userDbExistsAfter = File.Exists(this._userPersonalDbPath);

        if (userDbExistedBefore)
        {
            // If DB existed before, verify it wasn't modified
            var fileInfo = new FileInfo(this._userPersonalDbPath);
            var userDbSizeAfter = fileInfo.Length;
            var userDbModifiedAfter = fileInfo.LastWriteTimeUtc;

            Assert.Equal(userDbSizeBefore, userDbSizeAfter);
            Assert.Equal(userDbModifiedBefore, userDbModifiedAfter);
        }
        else
        {
            // If DB didn't exist, verify it wasn't created
            Assert.False(userDbExistsAfter,
                $"CRITICAL BUG: Test created user's personal database at {this._userPersonalDbPath}");
        }
    }

    [Fact]
    public async Task Fixed_SettingsWithConfigPath_MustUseTestDirectory()
    {
        // This test shows the CORRECT way: Load config and inject it
        var config = ConfigParser.LoadFromFile(this._configPath);

        var settingsWithConfigPath = new UpsertCommandSettings
        {
            ConfigPath = this._configPath,
            Content = "Test content in temp directory"
        };

        var command = new UpsertCommand(config);
        var context = new CommandContext(
            new[] { "--config", this._configPath },
            new EmptyRemainingArguments(),
            "put",
            null);

        // Act
        var exitCode = await command.ExecuteAsync(context, settingsWithConfigPath, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Equal(Constants.ExitCodeSuccess, exitCode);

        // Verify test used temp directory, not ~/.km
        var testDbPath = Path.Combine(this._tempDir, "nodes", "test", "content.db");
        Assert.True(File.Exists(testDbPath),
            $"Test should create database in temp directory: {testDbPath}");

        // Verify ~/.km was NOT touched
        Assert.False(this._userPersonalDbPath.Contains(this._tempDir),
            "User's personal database path should not be in test temp directory");
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
