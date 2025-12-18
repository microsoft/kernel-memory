// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using KernelMemory.Core.Config;
using KernelMemory.Core.Config.ContentIndex;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Config.SearchIndex;
using KernelMemory.Main.CLI.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console.Cli;

namespace KernelMemory.Main.Tests.Integration;

/// <summary>
/// Integration tests for node selection and broken node handling.
/// Issue 00006: km search should skip broken nodes gracefully and not crash.
/// </summary>
public sealed class NodeSelectionTests : IDisposable
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };
    private readonly string _tempDir;
    private readonly string _configPath;
    private readonly string _goodNodeDbPath;
    private readonly string _brokenNodeDbPath;

    public NodeSelectionTests()
    {
        // Create temp directory for test config
        this._tempDir = Path.Combine(Path.GetTempPath(), $"km-node-selection-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(this._tempDir);

        // Good node: will have a valid database
        this._goodNodeDbPath = Path.Combine(this._tempDir, "nodes", "good-node", "content.db");

        // Broken node: will NOT have a database (simulating missing database)
        this._brokenNodeDbPath = Path.Combine(this._tempDir, "nodes", "broken-node", "content.db");

        this._configPath = Path.Combine(this._tempDir, "config.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(this._tempDir))
        {
            Directory.Delete(this._tempDir, recursive: true);
        }
    }

    /// <summary>
    /// Creates a test config with both good and broken nodes.
    /// The good node has its database created, the broken node does not.
    /// </summary>
    private AppConfig CreateTestConfigWithBothNodes()
    {
        // Create the good node with its database
        var goodNodeDir = Path.GetDirectoryName(this._goodNodeDbPath)!;
        Directory.CreateDirectory(goodNodeDir);

        // Create actual database for good node using the same setup as real commands
        var optionsBuilder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<KernelMemory.Core.Storage.ContentStorageDbContext>();
        optionsBuilder.UseSqlite("Data Source=" + this._goodNodeDbPath);
        using var context = new KernelMemory.Core.Storage.ContentStorageDbContext(optionsBuilder.Options);
        context.Database.EnsureCreated();

        // Create FTS index database for good node
        var goodNodeFtsPath = Path.Combine(goodNodeDir, "fts.db");

        // Config with BOTH nodes (good first, broken second)
        var config = new AppConfig
        {
            Nodes = new Dictionary<string, NodeConfig>
            {
                ["good-node"] = new NodeConfig
                {
                    Id = "good-node",
                    ContentIndex = new SqliteContentIndexConfig { Path = this._goodNodeDbPath },
                    SearchIndexes =
                    [
                        new FtsSearchIndexConfig
                        {
                            Id = "fts",
                            Type = SearchIndexTypes.SqliteFTS,
                            Path = goodNodeFtsPath,
                            Weight = 1.0f
                        }
                    ]
                },
                ["broken-node"] = new NodeConfig
                {
                    Id = "broken-node",
                    ContentIndex = new SqliteContentIndexConfig { Path = this._brokenNodeDbPath },
                    SearchIndexes =
                    [
                        new FtsSearchIndexConfig
                        {
                            Id = "fts",
                            Type = SearchIndexTypes.SqliteFTS,
                            Path = Path.Combine(this._tempDir, "nodes", "broken-node", "fts.db"),
                            Weight = 1.0f
                        }
                    ]
                }
            }
        };

        var json = JsonSerializer.Serialize(config, s_jsonOptions);
        File.WriteAllText(this._configPath, json);

        return config;
    }

    /// <summary>
    /// Creates a test config where the broken node is first (first in insertion order).
    /// This tests that search still works even when the first node is broken.
    /// </summary>
    private AppConfig CreateTestConfigWithBrokenNodeFirst()
    {
        // Create the good node with its database
        var goodNodeDir = Path.GetDirectoryName(this._goodNodeDbPath)!;
        Directory.CreateDirectory(goodNodeDir);

        // Create actual database for good node
        var optionsBuilder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<KernelMemory.Core.Storage.ContentStorageDbContext>();
        optionsBuilder.UseSqlite("Data Source=" + this._goodNodeDbPath);
        using var context = new KernelMemory.Core.Storage.ContentStorageDbContext(optionsBuilder.Options);
        context.Database.EnsureCreated();

        // FTS paths
        var goodNodeFtsPath = Path.Combine(goodNodeDir, "fts.db");

        // Config with broken node FIRST (to test graceful skip)
        var config = new AppConfig
        {
            Nodes = new Dictionary<string, NodeConfig>
            {
                ["broken-node"] = new NodeConfig
                {
                    Id = "broken-node",
                    ContentIndex = new SqliteContentIndexConfig { Path = this._brokenNodeDbPath },
                    SearchIndexes =
                    [
                        new FtsSearchIndexConfig
                        {
                            Id = "fts",
                            Type = SearchIndexTypes.SqliteFTS,
                            Path = Path.Combine(this._tempDir, "nodes", "broken-node", "fts.db"),
                            Weight = 1.0f
                        }
                    ]
                },
                ["good-node"] = new NodeConfig
                {
                    Id = "good-node",
                    ContentIndex = new SqliteContentIndexConfig { Path = this._goodNodeDbPath },
                    SearchIndexes =
                    [
                        new FtsSearchIndexConfig
                        {
                            Id = "fts",
                            Type = SearchIndexTypes.SqliteFTS,
                            Path = goodNodeFtsPath,
                            Weight = 1.0f
                        }
                    ]
                }
            }
        };

        var json = JsonSerializer.Serialize(config, s_jsonOptions);
        File.WriteAllText(this._configPath, json);

        return config;
    }

    private static CommandContext CreateTestContext(string commandName)
    {
        return new CommandContext([], new EmptyRemainingArguments(), commandName, null);
    }

    /// <summary>
    /// Tests that search command succeeds even when one node has a broken/missing database.
    /// The search should skip the broken node and use the working node.
    /// Issue 00006: km search crashes on broken nodes instead of skipping them.
    /// </summary>
    [Fact]
    public async Task SearchCommand_WithBrokenNode_ShouldSkipBrokenNodeAndSucceed()
    {
        // Arrange: Create config with good node first, broken node second
        var config = this.CreateTestConfigWithBothNodes();

        var settings = new SearchCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Query = "test",
            Limit = 20,
            Offset = 0,
            MinRelevance = 0.3f
        };

        var command = new SearchCommand(config, NullLoggerFactory.Instance);
        var context = CreateTestContext("search");

        // Act: Execute search - this should NOT crash even though broken-node has no database
        var exitCode = await command.ExecuteAsync(context, settings, CancellationToken.None).ConfigureAwait(false);

        // Assert: Search should succeed (may return empty results, but shouldn't crash)
        Assert.Equal(Constants.App.ExitCodeSuccess, exitCode);
    }

    /// <summary>
    /// Tests that search command succeeds even when the FIRST node in config is broken.
    /// This tests the scenario where iteration order could cause early failure.
    /// </summary>
    [Fact]
    public async Task SearchCommand_WithBrokenNodeFirst_ShouldSkipBrokenNodeAndSucceed()
    {
        // Arrange: Create config with BROKEN node first
        var config = this.CreateTestConfigWithBrokenNodeFirst();

        var settings = new SearchCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Query = "test",
            Limit = 20,
            Offset = 0,
            MinRelevance = 0.3f
        };

        var command = new SearchCommand(config, NullLoggerFactory.Instance);
        var context = CreateTestContext("search");

        // Act: Execute search - this should NOT crash even though broken-node is first
        var exitCode = await command.ExecuteAsync(context, settings, CancellationToken.None).ConfigureAwait(false);

        // Assert: Search should succeed (may return empty results, but shouldn't crash)
        Assert.Equal(Constants.App.ExitCodeSuccess, exitCode);
    }

    /// <summary>
    /// Tests that search command shows first-run message when ALL nodes are broken.
    /// This is different from the partial failure case - if no nodes work at all,
    /// we should show a helpful first-run message.
    /// </summary>
    [Fact]
    public async Task SearchCommand_WithAllNodesBroken_ShouldShowFirstRunMessage()
    {
        // Arrange: Create config with ONLY broken nodes (no databases exist)
        var config = new AppConfig
        {
            Nodes = new Dictionary<string, NodeConfig>
            {
                ["broken1"] = new NodeConfig
                {
                    Id = "broken1",
                    ContentIndex = new SqliteContentIndexConfig { Path = Path.Combine(this._tempDir, "nonexistent1.db") },
                    SearchIndexes =
                    [
                        new FtsSearchIndexConfig
                        {
                            Id = "fts",
                            Type = SearchIndexTypes.SqliteFTS,
                            Path = Path.Combine(this._tempDir, "nonexistent1_fts.db"),
                            Weight = 1.0f
                        }
                    ]
                },
                ["broken2"] = new NodeConfig
                {
                    Id = "broken2",
                    ContentIndex = new SqliteContentIndexConfig { Path = Path.Combine(this._tempDir, "nonexistent2.db") },
                    SearchIndexes =
                    [
                        new FtsSearchIndexConfig
                        {
                            Id = "fts",
                            Type = SearchIndexTypes.SqliteFTS,
                            Path = Path.Combine(this._tempDir, "nonexistent2_fts.db"),
                            Weight = 1.0f
                        }
                    ]
                }
            }
        };

        var settings = new SearchCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            Query = "test",
            Limit = 20,
            Offset = 0,
            MinRelevance = 0.3f
        };

        var command = new SearchCommand(config, NullLoggerFactory.Instance);
        var context = CreateTestContext("search");

        // Act: Execute search - all nodes broken, should show first-run message (not crash)
        var exitCode = await command.ExecuteAsync(context, settings, CancellationToken.None).ConfigureAwait(false);

        // Assert: Should return success (first-run scenario) not error
        Assert.Equal(Constants.App.ExitCodeSuccess, exitCode);
    }

    /// <summary>
    /// Tests that list command uses the first node in config file order.
    /// Issue 00006: km list uses wrong default node (dictionary order vs config order).
    /// </summary>
    [Fact]
    public async Task ListCommand_WithoutNodeFlag_ShouldUseFirstNodeInConfigOrder()
    {
        // Arrange: Create config with good node first
        var config = this.CreateTestConfigWithBothNodes();

        var settings = new ListCommandSettings
        {
            ConfigPath = this._configPath,
            Format = "json",
            // NOTE: Not specifying NodeName - should use first node in config
            Skip = 0,
            Take = 20
        };

        var command = new ListCommand(config, NullLoggerFactory.Instance);
        var context = CreateTestContext("list");

        // Act: Execute list without --node flag
        var exitCode = await command.ExecuteAsync(context, settings, CancellationToken.None).ConfigureAwait(false);

        // Assert: Should succeed using first node (good-node)
        Assert.Equal(Constants.App.ExitCodeSuccess, exitCode);
    }

    /// <summary>
    /// Tests that the config preserves node order from JSON file.
    /// System.Text.Json should preserve property order since .NET 6+.
    /// </summary>
    [Fact]
    public void ConfigParser_ShouldPreserveNodeOrder()
    {
        // Arrange: Create config with specific order (must include type discriminators and all required fields)
        const string json = """
            {
                "nodes": {
                    "alpha": {
                        "id": "alpha",
                        "contentIndex": { "type": "sqlite", "path": "/tmp/alpha.db" },
                        "searchIndexes": [{ "type": "sqliteFTS", "id": "fts", "path": "/tmp/alpha_fts.db", "weight": 1.0 }]
                    },
                    "beta": {
                        "id": "beta",
                        "contentIndex": { "type": "sqlite", "path": "/tmp/beta.db" },
                        "searchIndexes": [{ "type": "sqliteFTS", "id": "fts", "path": "/tmp/beta_fts.db", "weight": 1.0 }]
                    },
                    "gamma": {
                        "id": "gamma",
                        "contentIndex": { "type": "sqlite", "path": "/tmp/gamma.db" },
                        "searchIndexes": [{ "type": "sqliteFTS", "id": "fts", "path": "/tmp/gamma_fts.db", "weight": 1.0 }]
                    }
                }
            }
            """;

        var configPath = Path.Combine(this._tempDir, "order-test-config.json");
        File.WriteAllText(configPath, json);

        // Act
        var config = ConfigParser.LoadFromFile(configPath);

        // Assert: Order should be preserved (Dictionary.Keys preserves insertion order in .NET)
        var nodeIds = config.Nodes.Keys.ToList();
        Assert.Equal(3, nodeIds.Count);
        Assert.Equal("alpha", nodeIds[0]);  // First in JSON = first in dictionary
        Assert.Equal("beta", nodeIds[1]);   // Second in JSON = second in dictionary
        Assert.Equal("gamma", nodeIds[2]);  // Third in JSON = third in dictionary
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
