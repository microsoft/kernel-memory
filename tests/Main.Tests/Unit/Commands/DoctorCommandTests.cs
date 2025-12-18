// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Core.Config;
using KernelMemory.Core.Config.Cache;
using KernelMemory.Core.Config.ContentIndex;
using KernelMemory.Core.Config.Embeddings;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Config.SearchIndex;
using KernelMemory.Main.CLI.Commands;
using Microsoft.Extensions.Logging;
using Moq;
using Spectre.Console.Cli;

namespace KernelMemory.Main.Tests.Unit.Commands;

/// <summary>
/// Unit tests for DoctorCommand validating configuration and system health checks.
/// Tests verify that doctor correctly identifies configuration issues and provides actionable fixes.
/// </summary>
public sealed class DoctorCommandTests
{
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger<DoctorCommand>> _mockLogger;

    public DoctorCommandTests()
    {
        this._mockLoggerFactory = new Mock<ILoggerFactory>();
        this._mockLogger = new Mock<ILogger<DoctorCommand>>();
        this._mockLoggerFactory
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(this._mockLogger.Object);
    }

    /// <summary>
    /// Verifies that doctor command succeeds when all dependencies are properly configured.
    /// This is the happy path test.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithValidConfigAndNoDependencies_ReturnsSuccess()
    {
        // Arrange - Create minimal config with only FTS (no external dependencies)
        var tempDir = Path.Combine(Path.GetTempPath(), $"km-doctor-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var config = new AppConfig
            {
                Nodes = new Dictionary<string, NodeConfig>
                {
                    ["test"] = new NodeConfig
                    {
                        Id = "test",
                        Access = NodeAccessLevels.Full,
                        ContentIndex = new SqliteContentIndexConfig
                        {
                            Path = Path.Combine(tempDir, "content.db")
                        },
                        SearchIndexes = new List<SearchIndexConfig>
                        {
                            new FtsSearchIndexConfig
                            {
                                Id = "fts",
                                Type = SearchIndexTypes.SqliteFTS,
                                Path = Path.Combine(tempDir, "fts.db")
                            }
                        }
                    }
                }
            };

            using var command = new DoctorCommand(config, this._mockLoggerFactory.Object);
            var settings = new DoctorCommandSettings { NoColor = true, Format = "json" };
            var cliContext = new CommandContext([], new EmptyRemainingArguments(), "doctor", null!);

            // Act
            var exitCode = await command.ExecuteAsync(cliContext, settings, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Equal(0, exitCode);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Verifies that doctor detects missing Ollama and returns error exit code.
    /// Tests the critical use case of vector search with unavailable provider.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithOllamaConfigButServerDown_ReturnsError()
    {
        // Arrange - Config with Ollama vector index (Ollama likely not running on port 9999)
        var tempDir = Path.Combine(Path.GetTempPath(), $"km-doctor-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var config = new AppConfig
            {
                Nodes = new Dictionary<string, NodeConfig>
                {
                    ["test"] = new NodeConfig
                    {
                        Id = "test",
                        Access = NodeAccessLevels.Full,
                        ContentIndex = new SqliteContentIndexConfig
                        {
                            Path = Path.Combine(tempDir, "content.db")
                        },
                        SearchIndexes = new List<SearchIndexConfig>
                        {
                            new VectorSearchIndexConfig
                            {
                                Id = "vector",
                                Type = SearchIndexTypes.SqliteVector,
                                Path = Path.Combine(tempDir, "vector.db"),
                                Dimensions = 1024,
                                Embeddings = new OllamaEmbeddingsConfig
                                {
                                    Model = "qwen3-embedding",
                                    BaseUrl = "http://localhost:9999" // Non-existent port
                                }
                            }
                        }
                    }
                }
            };

            using var command = new DoctorCommand(config, this._mockLoggerFactory.Object);
            var settings = new DoctorCommandSettings { NoColor = true, Format = "json" };
            var cliContext = new CommandContext([], new EmptyRemainingArguments(), "doctor", null!);

            // Act
            var exitCode = await command.ExecuteAsync(cliContext, settings, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Equal(1, exitCode); // User error - configuration issue
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Verifies that doctor detects missing OpenAI API key.
    /// Tests that environment variable checks work correctly.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithOpenAIButNoApiKey_ReturnsError()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"km-doctor-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        // Save and clear the OPENAI_API_KEY environment variable for this test
        var originalApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);

        try
        {
            var config = new AppConfig
            {
                Nodes = new Dictionary<string, NodeConfig>
                {
                    ["test"] = new NodeConfig
                    {
                        Id = "test",
                        Access = NodeAccessLevels.Full,
                        ContentIndex = new SqliteContentIndexConfig
                        {
                            Path = Path.Combine(tempDir, "content.db")
                        },
                        SearchIndexes = new List<SearchIndexConfig>
                        {
                            new VectorSearchIndexConfig
                            {
                                Id = "vector",
                                Type = SearchIndexTypes.SqliteVector,
                                Path = Path.Combine(tempDir, "vector.db"),
                                Dimensions = 1536,
                                Embeddings = new OpenAIEmbeddingsConfig
                                {
                                    Model = "text-embedding-3-small"
                                    // No ApiKey set, and OPENAI_API_KEY env var cleared above
                                }
                            }
                        }
                    }
                }
            };

            using var command = new DoctorCommand(config, this._mockLoggerFactory.Object);
            var settings = new DoctorCommandSettings { NoColor = true, Format = "json" };
            var cliContext = new CommandContext([], new EmptyRemainingArguments(), "doctor", null!);

            // Act
            var exitCode = await command.ExecuteAsync(cliContext, settings, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Equal(1, exitCode); // Error exit code
        }
        finally
        {
            // Restore the original environment variable
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", originalApiKey);

            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Verifies that doctor warns about missing cache file but doesn't fail.
    /// Cache will be created on first use, so this is a warning not an error.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithMissingCacheFile_ReturnsSuccessWithWarning()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"km-doctor-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var config = new AppConfig
            {
                Nodes = new Dictionary<string, NodeConfig>
                {
                    ["test"] = new NodeConfig
                    {
                        Id = "test",
                        Access = NodeAccessLevels.Full,
                        ContentIndex = new SqliteContentIndexConfig
                        {
                            Path = Path.Combine(tempDir, "content.db")
                        },
                        SearchIndexes = new List<SearchIndexConfig>
                        {
                            new FtsSearchIndexConfig
                            {
                                Id = "fts",
                                Type = SearchIndexTypes.SqliteFTS,
                                Path = Path.Combine(tempDir, "fts.db")
                            }
                        }
                    }
                },
                EmbeddingsCache = new CacheConfig
                {
                    Path = Path.Combine(tempDir, "cache.db"),
                    AllowRead = true,
                    AllowWrite = true,
                    Type = CacheTypes.Sqlite
                }
            };

            using var command = new DoctorCommand(config, this._mockLoggerFactory.Object);
            var settings = new DoctorCommandSettings { NoColor = true, Format = "json" };
            var cliContext = new CommandContext([], new EmptyRemainingArguments(), "doctor", null!);

            // Act
            var exitCode = await command.ExecuteAsync(cliContext, settings, CancellationToken.None).ConfigureAwait(false);

            // Assert - Should succeed (warnings don't cause failure, only errors do)
            Assert.Equal(0, exitCode);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Verifies that doctor correctly groups output by node when multiple nodes are configured.
    /// Tests that NodeId is properly set for node-specific checks (Content index, FTS index).
    /// Global checks (Config file, cache) should have null NodeId.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithMultipleNodes_GroupsOutputByNode()
    {
        // Arrange - Create config with 3 nodes for thorough testing
        var tempDir = Path.Combine(Path.GetTempPath(), $"km-doctor-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create node directories
            var node1Dir = Path.Combine(tempDir, "node1");
            var node2Dir = Path.Combine(tempDir, "node2");
            var node3Dir = Path.Combine(tempDir, "node3");
            Directory.CreateDirectory(node1Dir);
            Directory.CreateDirectory(node2Dir);
            Directory.CreateDirectory(node3Dir);

            var config = new AppConfig
            {
                Nodes = new Dictionary<string, NodeConfig>
                {
                    ["personal"] = new NodeConfig
                    {
                        Id = "personal",
                        Access = NodeAccessLevels.Full,
                        ContentIndex = new SqliteContentIndexConfig
                        {
                            Path = Path.Combine(node1Dir, "content.db")
                        },
                        SearchIndexes = new List<SearchIndexConfig>
                        {
                            new FtsSearchIndexConfig
                            {
                                Id = "fts-1",
                                Type = SearchIndexTypes.SqliteFTS,
                                Path = Path.Combine(node1Dir, "fts.db")
                            }
                        }
                    },
                    ["work"] = new NodeConfig
                    {
                        Id = "work",
                        Access = NodeAccessLevels.Full,
                        ContentIndex = new SqliteContentIndexConfig
                        {
                            Path = Path.Combine(node2Dir, "content.db")
                        },
                        SearchIndexes = new List<SearchIndexConfig>
                        {
                            new FtsSearchIndexConfig
                            {
                                Id = "fts-2",
                                Type = SearchIndexTypes.SqliteFTS,
                                Path = Path.Combine(node2Dir, "fts.db")
                            }
                        }
                    },
                    ["archive"] = new NodeConfig
                    {
                        Id = "archive",
                        Access = NodeAccessLevels.ReadOnly,
                        ContentIndex = new SqliteContentIndexConfig
                        {
                            Path = Path.Combine(node3Dir, "content.db")
                        },
                        SearchIndexes = new List<SearchIndexConfig>
                        {
                            new FtsSearchIndexConfig
                            {
                                Id = "fts-3",
                                Type = SearchIndexTypes.SqliteFTS,
                                Path = Path.Combine(node3Dir, "fts.db")
                            }
                        }
                    }
                },
                EmbeddingsCache = new CacheConfig
                {
                    Path = Path.Combine(tempDir, "cache.db"),
                    AllowRead = true,
                    AllowWrite = true,
                    Type = CacheTypes.Sqlite
                }
            };

            using var command = new DoctorCommand(config, this._mockLoggerFactory.Object);
            var settings = new DoctorCommandSettings { NoColor = true, Format = "json" };
            var cliContext = new CommandContext([], new EmptyRemainingArguments(), "doctor", null!);

            // Capture console output to verify JSON output contains nodeId
            var originalOut = Console.Out;
            using var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            try
            {
                // Act
                var exitCode = await command.ExecuteAsync(cliContext, settings, CancellationToken.None).ConfigureAwait(false);

                // Assert - Should succeed (only FTS, no external dependencies)
                Assert.Equal(0, exitCode);

                // Parse JSON output to verify nodeId is set correctly
                var output = stringWriter.ToString();
                using var doc = System.Text.Json.JsonDocument.Parse(output);
                var root = doc.RootElement;

                Assert.True(root.TryGetProperty("results", out var results));
                var resultsList = results.EnumerateArray().ToList();

                // Should have checks for: config file (1) + 3 nodes x 2 checks (content + FTS) + 1 cache = 8 checks
                Assert.Equal(8, resultsList.Count);

                // Verify config file check has no nodeId (null is omitted in JSON)
                var configCheck = resultsList.First(r => r.GetProperty("component").GetString() == "Config file");
                // null values are omitted due to JsonIgnoreCondition.WhenWritingNull
                Assert.False(configCheck.TryGetProperty("nodeId", out _));

                // Verify cache check has no nodeId (null is omitted in JSON)
                var cacheCheck = resultsList.First(r => r.GetProperty("component").GetString() == "Embeddings cache");
                Assert.False(cacheCheck.TryGetProperty("nodeId", out _));

                // Verify node-specific checks have correct nodeId
                var nodeChecks = resultsList.Where(r =>
                    r.TryGetProperty("nodeId", out var nid) &&
                    nid.ValueKind == System.Text.Json.JsonValueKind.String).ToList();

                // Should have 6 node-specific checks (3 nodes x 2 checks each)
                Assert.Equal(6, nodeChecks.Count);

                // Verify each node has both content and FTS checks
                var personalChecks = nodeChecks.Where(r => r.GetProperty("nodeId").GetString() == "personal").ToList();
                var workChecks = nodeChecks.Where(r => r.GetProperty("nodeId").GetString() == "work").ToList();
                var archiveChecks = nodeChecks.Where(r => r.GetProperty("nodeId").GetString() == "archive").ToList();

                Assert.Equal(2, personalChecks.Count);
                Assert.Equal(2, workChecks.Count);
                Assert.Equal(2, archiveChecks.Count);

                // Verify summary
                Assert.True(root.TryGetProperty("summary", out var summary));
                Assert.Equal(8, summary.GetProperty("total").GetInt32());
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}

/// <summary>
/// Empty IRemainingArguments implementation for CommandContext in tests.
/// </summary>
internal sealed class EmptyRemainingArguments : IRemainingArguments
{
    public IReadOnlyList<string> Raw => Array.Empty<string>();
    public ILookup<string, string?> Parsed => Enumerable.Empty<string>().ToLookup(x => x, x => (string?)null);
}
