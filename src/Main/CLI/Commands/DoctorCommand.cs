// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using KernelMemory.Core;
using KernelMemory.Core.Config;
using KernelMemory.Core.Config.Cache;
using KernelMemory.Core.Config.ContentIndex;
using KernelMemory.Core.Config.Embeddings;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Config.SearchIndex;
using KernelMemory.Main.CLI.Infrastructure;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace KernelMemory.Main.CLI.Commands;

/// <summary>
/// Diagnostic levels for health checks.
/// OK = working, Warning = will work but suboptimal, Error = will not work.
/// </summary>
public enum DiagnosticLevels
{
    OK,
    Warning,
    Error
}

/// <summary>
/// Result of a single diagnostic check.
/// Includes component name, status, message, and optional node association.
/// </summary>
public sealed record DiagnosticResult
{
    /// <summary>
    /// Name of the component being checked (e.g., "Config file", "Content index").
    /// </summary>
    public required string Component { get; init; }

    /// <summary>
    /// Diagnostic level indicating severity.
    /// </summary>
    public required DiagnosticLevels Level { get; init; }

    /// <summary>
    /// Human-readable description of the check result.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Node ID this check belongs to, or null for global checks.
    /// Used for grouping output by node.
    /// </summary>
    public string? NodeId { get; init; }
}

/// <summary>
/// Command to validate configuration and check system health.
/// Checks config file, content indexes, search indexes (FTS/vector), and caches.
/// Groups output by node for clarity when multiple nodes are configured.
/// </summary>
public sealed class DoctorCommand : AsyncCommand<DoctorCommandSettings>, IDisposable
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly AppConfig _config;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<DoctorCommand> _logger;
    private readonly ConfigPathService _configPathService;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="DoctorCommand"/> class.
    /// </summary>
    /// <param name="config">Application configuration (injected by DI).</param>
    /// <param name="loggerFactory">Logger factory for creating loggers (injected by DI).</param>
    /// <param name="configPathService">Service providing the config file path (injected by DI).</param>
    public DoctorCommand(
        AppConfig config,
        ILoggerFactory loggerFactory,
        ConfigPathService configPathService)
    {
        this._config = config ?? throw new ArgumentNullException(nameof(config));
        this._loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        this._logger = loggerFactory.CreateLogger<DoctorCommand>();
        this._configPathService = configPathService ?? throw new ArgumentNullException(nameof(configPathService));
        this._httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    /// <summary>
    /// Constructor for testing without ConfigPathService.
    /// Uses a default config path for diagnostic purposes.
    /// </summary>
    /// <param name="config">Application configuration.</param>
    /// <param name="loggerFactory">Logger factory for creating loggers.</param>
    internal DoctorCommand(AppConfig config, ILoggerFactory loggerFactory)
        : this(config, loggerFactory, new ConfigPathService(GetDefaultConfigPath()))
    {
    }

    /// <summary>
    /// Disposes the HTTP client.
    /// </summary>
    public void Dispose()
    {
        this._httpClient.Dispose();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Top-level command handler must catch all exceptions to return appropriate exit codes")]
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DoctorCommandSettings settings,
        CancellationToken cancellationToken)
    {
        var results = new List<DiagnosticResult>();

        try
        {
            // Global check: Config file
            results.Add(this.CheckConfigFile());

            // Per-node checks
            foreach (var (nodeId, nodeConfig) in this._config.Nodes)
            {
                // Content index check
                results.Add(this.CheckContentIndex(nodeId, nodeConfig.ContentIndex));

                // Search index checks
                foreach (var searchIndex in nodeConfig.SearchIndexes)
                {
                    var indexResult = await this.CheckSearchIndexAsync(nodeId, searchIndex, cancellationToken)
                        .ConfigureAwait(false);
                    results.Add(indexResult);
                }
            }

            // Global check: Embeddings cache
            if (this._config.EmbeddingsCache != null)
            {
                results.Add(this.CheckCache("Embeddings cache", this._config.EmbeddingsCache));
            }

            // Global check: LLM cache
            if (this._config.LLMCache != null)
            {
                results.Add(this.CheckCache("LLM cache", this._config.LLMCache));
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Unexpected error during doctor checks");
            results.Add(new DiagnosticResult
            {
                Component = "Doctor command",
                Level = DiagnosticLevels.Error,
                Message = $"Unexpected error: {ex.Message}"
            });
        }

        // Display results
        this.DisplayResults(results, settings);

        // Return appropriate exit code
        var hasErrors = results.Any(r => r.Level == DiagnosticLevels.Error);
        return hasErrors ? Constants.App.ExitCodeUserError : Constants.App.ExitCodeSuccess;
    }

    /// <summary>
    /// Checks the configuration file accessibility.
    /// </summary>
    private DiagnosticResult CheckConfigFile()
    {
        var configPath = this._configPathService.Path;

        if (!File.Exists(configPath))
        {
            return new DiagnosticResult
            {
                Component = "Config file",
                Level = DiagnosticLevels.Warning,
                Message = $"Using default configuration, file does not exist: {configPath}"
            };
        }

        try
        {
            var fileInfo = new FileInfo(configPath);

            // Actually test read access by opening the file
            using var stream = File.OpenRead(configPath);
            var canRead = stream.CanRead;

            if (!canRead)
            {
                return new DiagnosticResult
                {
                    Component = "Config file",
                    Level = DiagnosticLevels.Error,
                    Message = $"Cannot read config file: {configPath}"
                };
            }

            return new DiagnosticResult
            {
                Component = "Config file",
                Level = DiagnosticLevels.OK,
                Message = $"{configPath} readable ({fileInfo.Length} bytes)"
            };
        }
        catch (UnauthorizedAccessException)
        {
            return new DiagnosticResult
            {
                Component = "Config file",
                Level = DiagnosticLevels.Error,
                Message = $"Permission denied reading config file: {configPath}"
            };
        }
        catch (IOException ex)
        {
            return new DiagnosticResult
            {
                Component = "Config file",
                Level = DiagnosticLevels.Error,
                Message = $"Error reading config file: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Checks the content index (SQLite database) accessibility.
    /// </summary>
    private DiagnosticResult CheckContentIndex(string nodeId, ContentIndexConfig config)
    {
        if (config is not SqliteContentIndexConfig sqliteConfig)
        {
            return new DiagnosticResult
            {
                Component = "Content index",
                Level = DiagnosticLevels.Error,
                Message = $"Unsupported content index type: {config.Type}",
                NodeId = nodeId
            };
        }

        var dbPath = sqliteConfig.Path;
        var dirPath = Path.GetDirectoryName(dbPath);

        if (File.Exists(dbPath))
        {
            // Database exists - test read/write access
            try
            {
                using var stream = File.Open(dbPath, FileMode.Open, FileAccess.ReadWrite);
                return new DiagnosticResult
                {
                    Component = "Content index",
                    Level = DiagnosticLevels.OK,
                    Message = $"Content database readable at {dbPath}",
                    NodeId = nodeId
                };
            }
            catch (UnauthorizedAccessException)
            {
                return new DiagnosticResult
                {
                    Component = "Content index",
                    Level = DiagnosticLevels.Error,
                    Message = $"Permission denied accessing database: {dbPath}",
                    NodeId = nodeId
                };
            }
            catch (IOException ex)
            {
                return new DiagnosticResult
                {
                    Component = "Content index",
                    Level = DiagnosticLevels.Error,
                    Message = $"Error accessing database: {ex.Message}",
                    NodeId = nodeId
                };
            }
        }

        // Database doesn't exist - check if directory is writable
        if (!string.IsNullOrEmpty(dirPath))
        {
            if (!Directory.Exists(dirPath))
            {
                // Try to create the directory to test permissions
                try
                {
                    Directory.CreateDirectory(dirPath);
                    return new DiagnosticResult
                    {
                        Component = "Content index",
                        Level = DiagnosticLevels.Warning,
                        Message = $"Directory writable, content database will be created at {dbPath}",
                        NodeId = nodeId
                    };
                }
                catch (UnauthorizedAccessException)
                {
                    return new DiagnosticResult
                    {
                        Component = "Content index",
                        Level = DiagnosticLevels.Error,
                        Message = $"Permission denied creating directory: {dirPath}",
                        NodeId = nodeId
                    };
                }
                catch (IOException ex)
                {
                    return new DiagnosticResult
                    {
                        Component = "Content index",
                        Level = DiagnosticLevels.Error,
                        Message = $"Error creating directory: {ex.Message}",
                        NodeId = nodeId
                    };
                }
            }

            // Directory exists - test write permissions by creating a temp file
            var canWrite = this.CanWriteToDirectory(dirPath);
            if (canWrite)
            {
                return new DiagnosticResult
                {
                    Component = "Content index",
                    Level = DiagnosticLevels.Warning,
                    Message = $"Directory writable, content database will be created at {dbPath}",
                    NodeId = nodeId
                };
            }

            return new DiagnosticResult
            {
                Component = "Content index",
                Level = DiagnosticLevels.Error,
                Message = $"Directory not writable: {dirPath}",
                NodeId = nodeId
            };
        }

        return new DiagnosticResult
        {
            Component = "Content index",
            Level = DiagnosticLevels.Error,
            Message = "Invalid database path configuration",
            NodeId = nodeId
        };
    }

    /// <summary>
    /// Checks a search index configuration and connectivity.
    /// </summary>
    private async Task<DiagnosticResult> CheckSearchIndexAsync(
        string nodeId,
        SearchIndexConfig config,
        CancellationToken cancellationToken)
    {
        return config switch
        {
            FtsSearchIndexConfig ftsConfig => this.CheckFtsIndex(nodeId, ftsConfig),
            VectorSearchIndexConfig vectorConfig => await this.CheckVectorIndexAsync(nodeId, vectorConfig, cancellationToken)
                .ConfigureAwait(false),
            _ => new DiagnosticResult
            {
                Component = $"Search index '{config.Id}'",
                Level = DiagnosticLevels.Warning,
                Message = $"Unknown search index type: {config.GetType().Name}",
                NodeId = nodeId
            }
        };
    }

    /// <summary>
    /// Checks an FTS index (SQLite FTS5 database) accessibility.
    /// </summary>
    private DiagnosticResult CheckFtsIndex(string nodeId, FtsSearchIndexConfig config)
    {
        var dbPath = config.Path;
        if (string.IsNullOrEmpty(dbPath))
        {
            return new DiagnosticResult
            {
                Component = $"FTS index '{config.Id}'",
                Level = DiagnosticLevels.Error,
                Message = "FTS index path not configured",
                NodeId = nodeId
            };
        }

        var dirPath = Path.GetDirectoryName(dbPath);

        if (File.Exists(dbPath))
        {
            try
            {
                using var stream = File.Open(dbPath, FileMode.Open, FileAccess.ReadWrite);
                return new DiagnosticResult
                {
                    Component = $"FTS index '{config.Id}'",
                    Level = DiagnosticLevels.OK,
                    Message = $"FTS database readable at {dbPath}",
                    NodeId = nodeId
                };
            }
            catch (UnauthorizedAccessException)
            {
                return new DiagnosticResult
                {
                    Component = $"FTS index '{config.Id}'",
                    Level = DiagnosticLevels.Error,
                    Message = $"Permission denied accessing FTS database: {dbPath}",
                    NodeId = nodeId
                };
            }
            catch (IOException ex)
            {
                return new DiagnosticResult
                {
                    Component = $"FTS index '{config.Id}'",
                    Level = DiagnosticLevels.Error,
                    Message = $"Error accessing FTS database: {ex.Message}",
                    NodeId = nodeId
                };
            }
        }

        // Database doesn't exist - check directory write permissions
        if (!string.IsNullOrEmpty(dirPath))
        {
            if (!Directory.Exists(dirPath))
            {
                try
                {
                    Directory.CreateDirectory(dirPath);
                    return new DiagnosticResult
                    {
                        Component = $"FTS index '{config.Id}'",
                        Level = DiagnosticLevels.Warning,
                        Message = $"Directory writable, FTS database will be created at {dbPath}",
                        NodeId = nodeId
                    };
                }
                catch (Exception ex)
                {
                    return new DiagnosticResult
                    {
                        Component = $"FTS index '{config.Id}'",
                        Level = DiagnosticLevels.Error,
                        Message = $"Cannot create directory: {ex.Message}",
                        NodeId = nodeId
                    };
                }
            }

            var canWrite = this.CanWriteToDirectory(dirPath);
            if (canWrite)
            {
                return new DiagnosticResult
                {
                    Component = $"FTS index '{config.Id}'",
                    Level = DiagnosticLevels.Warning,
                    Message = $"Directory writable, FTS database will be created at {dbPath}",
                    NodeId = nodeId
                };
            }

            return new DiagnosticResult
            {
                Component = $"FTS index '{config.Id}'",
                Level = DiagnosticLevels.Error,
                Message = $"Directory not writable: {dirPath}",
                NodeId = nodeId
            };
        }

        return new DiagnosticResult
        {
            Component = $"FTS index '{config.Id}'",
            Level = DiagnosticLevels.Error,
            Message = "Invalid FTS database path configuration",
            NodeId = nodeId
        };
    }

    /// <summary>
    /// Checks a vector index, including embeddings provider connectivity.
    /// </summary>
    private async Task<DiagnosticResult> CheckVectorIndexAsync(
        string nodeId,
        VectorSearchIndexConfig config,
        CancellationToken cancellationToken)
    {
        // First check database accessibility
        var dbPath = config.Path;
        if (string.IsNullOrEmpty(dbPath))
        {
            return new DiagnosticResult
            {
                Component = $"Vector index '{config.Id}'",
                Level = DiagnosticLevels.Error,
                Message = "Vector index path not configured",
                NodeId = nodeId
            };
        }

        // Check embeddings provider if configured
        if (config.Embeddings == null)
        {
            return new DiagnosticResult
            {
                Component = $"Vector index '{config.Id}'",
                Level = DiagnosticLevels.Error,
                Message = "No embeddings provider configured for vector index",
                NodeId = nodeId
            };
        }

        return config.Embeddings switch
        {
            OllamaEmbeddingsConfig ollamaConfig => await this.CheckOllamaEmbeddingsAsync(
                nodeId, config.Id, ollamaConfig, config.Dimensions, cancellationToken).ConfigureAwait(false),
            OpenAIEmbeddingsConfig openAiConfig => this.CheckOpenAIEmbeddings(nodeId, config.Id, openAiConfig, config.Dimensions),
            _ => new DiagnosticResult
            {
                Component = $"Vector index '{config.Id}'",
                Level = DiagnosticLevels.Warning,
                Message = $"Unsupported embeddings provider: {config.Embeddings.GetType().Name}",
                NodeId = nodeId
            }
        };
    }

    /// <summary>
    /// Checks Ollama embeddings provider by actually calling the API.
    /// </summary>
    private async Task<DiagnosticResult> CheckOllamaEmbeddingsAsync(
        string nodeId,
        string indexId,
        OllamaEmbeddingsConfig config,
        int expectedDimensions,
        CancellationToken cancellationToken)
    {
        try
        {
            // Actually test embedding generation with a POST request
            var endpoint = $"{config.BaseUrl.TrimEnd('/')}/api/embed";
            var request = new { model = config.Model, input = "test" };

            using var response = await this._httpClient.PostAsJsonAsync(endpoint, request, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return new DiagnosticResult
                {
                    Component = $"Vector index '{indexId}'",
                    Level = DiagnosticLevels.Error,
                    Message = $"Ollama API error ({response.StatusCode}): {errorContent.Substring(0, Math.Min(100, errorContent.Length))}",
                    NodeId = nodeId
                };
            }

            // Parse response to verify dimensions
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(responseJson);

            if (doc.RootElement.TryGetProperty("embeddings", out var embeddingsArray) &&
                embeddingsArray.GetArrayLength() > 0)
            {
                var firstEmbedding = embeddingsArray[0];
                var actualDimensions = firstEmbedding.GetArrayLength();

                if (actualDimensions != expectedDimensions)
                {
                    return new DiagnosticResult
                    {
                        Component = $"Vector index '{indexId}'",
                        Level = DiagnosticLevels.Error,
                        Message = $"Dimension mismatch: model produces {actualDimensions}D, config expects {expectedDimensions}D",
                        NodeId = nodeId
                    };
                }

                return new DiagnosticResult
                {
                    Component = $"Vector index '{indexId}'",
                    Level = DiagnosticLevels.OK,
                    Message = $"Ollama embeddings working ({config.Model}, {actualDimensions}D)",
                    NodeId = nodeId
                };
            }

            return new DiagnosticResult
            {
                Component = $"Vector index '{indexId}'",
                Level = DiagnosticLevels.Warning,
                Message = "Ollama responded but could not verify dimensions",
                NodeId = nodeId
            };
        }
        catch (HttpRequestException ex)
        {
            return new DiagnosticResult
            {
                Component = $"Vector index '{indexId}'",
                Level = DiagnosticLevels.Error,
                Message = $"Cannot connect to Ollama at {config.BaseUrl}: {ex.Message}",
                NodeId = nodeId
            };
        }
        catch (TaskCanceledException)
        {
            return new DiagnosticResult
            {
                Component = $"Vector index '{indexId}'",
                Level = DiagnosticLevels.Error,
                Message = $"Timeout connecting to Ollama at {config.BaseUrl}",
                NodeId = nodeId
            };
        }
        catch (JsonException ex)
        {
            return new DiagnosticResult
            {
                Component = $"Vector index '{indexId}'",
                Level = DiagnosticLevels.Warning,
                Message = $"Ollama responded but response parsing failed: {ex.Message}",
                NodeId = nodeId
            };
        }
    }

    /// <summary>
    /// Checks OpenAI embeddings configuration (API key presence, not connectivity).
    /// </summary>
    private DiagnosticResult CheckOpenAIEmbeddings(
        string nodeId,
        string indexId,
        OpenAIEmbeddingsConfig config,
        int expectedDimensions)
    {
        // Check if API key is configured
        var apiKey = config.ApiKey;

        // Try environment variable if not set directly
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new DiagnosticResult
            {
                Component = $"Vector index '{indexId}'",
                Level = DiagnosticLevels.Error,
                Message = "OpenAI API key not configured (set in config or OPENAI_API_KEY env var)",
                NodeId = nodeId
            };
        }

        // Verify dimensions match known model dimensions
        if (Constants.EmbeddingDefaults.TryGetDimensions(config.Model, out var knownDimensions))
        {
            if (knownDimensions != expectedDimensions)
            {
                return new DiagnosticResult
                {
                    Component = $"Vector index '{indexId}'",
                    Level = DiagnosticLevels.Error,
                    Message = $"Dimension mismatch: {config.Model} produces {knownDimensions}D, config expects {expectedDimensions}D",
                    NodeId = nodeId
                };
            }
        }

        return new DiagnosticResult
        {
            Component = $"Vector index '{indexId}'",
            Level = DiagnosticLevels.OK,
            Message = $"OpenAI API key configured ({config.Model}, {expectedDimensions}D)",
            NodeId = nodeId
        };
    }

    /// <summary>
    /// Checks cache configuration and accessibility.
    /// </summary>
    private DiagnosticResult CheckCache(string name, CacheConfig config)
    {
        if (config.Type != CacheTypes.Sqlite || string.IsNullOrEmpty(config.Path))
        {
            return new DiagnosticResult
            {
                Component = name,
                Level = DiagnosticLevels.Warning,
                Message = "Non-SQLite cache or missing path"
            };
        }

        var dbPath = config.Path;
        var dirPath = Path.GetDirectoryName(dbPath);

        if (File.Exists(dbPath))
        {
            try
            {
                using var stream = File.Open(dbPath, FileMode.Open, FileAccess.ReadWrite);
                return new DiagnosticResult
                {
                    Component = name,
                    Level = DiagnosticLevels.OK,
                    Message = $"Cache database readable at {dbPath}"
                };
            }
            catch (Exception ex)
            {
                return new DiagnosticResult
                {
                    Component = name,
                    Level = DiagnosticLevels.Error,
                    Message = $"Error accessing cache: {ex.Message}"
                };
            }
        }

        // Database doesn't exist - check directory write permissions
        if (!string.IsNullOrEmpty(dirPath))
        {
            if (!Directory.Exists(dirPath))
            {
                try
                {
                    Directory.CreateDirectory(dirPath);
                    return new DiagnosticResult
                    {
                        Component = name,
                        Level = DiagnosticLevels.Warning,
                        Message = $"Directory writable, cache database will be created at {dbPath}"
                    };
                }
                catch (Exception ex)
                {
                    return new DiagnosticResult
                    {
                        Component = name,
                        Level = DiagnosticLevels.Error,
                        Message = $"Cannot create directory: {ex.Message}"
                    };
                }
            }

            var canWrite = this.CanWriteToDirectory(dirPath);
            if (canWrite)
            {
                return new DiagnosticResult
                {
                    Component = name,
                    Level = DiagnosticLevels.Warning,
                    Message = $"Directory writable, cache database will be created at {dbPath}"
                };
            }

            return new DiagnosticResult
            {
                Component = name,
                Level = DiagnosticLevels.Error,
                Message = $"Directory not writable: {dirPath}"
            };
        }

        return new DiagnosticResult
        {
            Component = name,
            Level = DiagnosticLevels.Error,
            Message = "Invalid cache path configuration"
        };
    }

    /// <summary>
    /// Tests if a directory is writable by creating and deleting a temp file.
    /// </summary>
    private bool CanWriteToDirectory(string dirPath)
    {
        try
        {
            var testFile = Path.Combine(dirPath, $".km-doctor-test-{Guid.NewGuid()}");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Displays results, grouped by node for clarity.
    /// </summary>
    private void DisplayResults(List<DiagnosticResult> results, DoctorCommandSettings settings)
    {
        // JSON output
        if (settings.Format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            this.DisplayResultsAsJson(results);
            return;
        }

        // Human-readable output
        this.DisplayResultsGroupedByNode(results, settings);
    }

    /// <summary>
    /// Displays results in JSON format.
    /// </summary>
    private void DisplayResultsAsJson(List<DiagnosticResult> results)
    {
        var output = new
        {
            results = results.Select(r => new
            {
                component = r.Component,
                level = r.Level.ToString().ToLowerInvariant(),
                message = r.Message,
                nodeId = r.NodeId
            }).ToList(),
            summary = new
            {
                total = results.Count,
                ok = results.Count(r => r.Level == DiagnosticLevels.OK),
                warnings = results.Count(r => r.Level == DiagnosticLevels.Warning),
                errors = results.Count(r => r.Level == DiagnosticLevels.Error)
            }
        };

        var json = JsonSerializer.Serialize(output, s_jsonOptions);
        Console.WriteLine(json);
    }

    /// <summary>
    /// Displays results grouped by node with visual formatting.
    /// Global checks (NodeId == null) are shown at the top and bottom.
    /// Node-specific checks are indented under node headers.
    /// </summary>
    private void DisplayResultsGroupedByNode(List<DiagnosticResult> results, DoctorCommandSettings settings)
    {
        var useColor = !settings.NoColor;

        // Separate global checks and node-specific checks
        var globalChecks = results.Where(r => r.NodeId == null).ToList();
        var nodeGroups = results
            .Where(r => r.NodeId != null)
            .GroupBy(r => r.NodeId!)
            .OrderBy(g => g.Key)
            .ToList();

        // Display global checks first (config file)
        var configCheck = globalChecks.FirstOrDefault(r => r.Component == "Config file");
        if (configCheck != null)
        {
            this.DisplayCheck(configCheck, indent: 0, useColor);
            AnsiConsole.WriteLine();
        }

        // Display node-grouped checks
        foreach (var nodeGroup in nodeGroups)
        {
            // Node header in bold
            if (useColor)
            {
                AnsiConsole.MarkupLine($"[bold]Node '{Markup.Escape(nodeGroup.Key)}':[/]");
            }
            else
            {
                Console.WriteLine($"Node '{nodeGroup.Key}':");
            }

            // Indented checks for this node
            foreach (var check in nodeGroup)
            {
                this.DisplayCheck(check, indent: 2, useColor);
            }

            AnsiConsole.WriteLine();
        }

        // Display remaining global checks (caches)
        foreach (var check in globalChecks.Where(r => r.Component != "Config file"))
        {
            this.DisplayCheck(check, indent: 0, useColor);
        }

        // Summary line
        var errorCount = results.Count(r => r.Level == DiagnosticLevels.Error);
        var warningCount = results.Count(r => r.Level == DiagnosticLevels.Warning);

        AnsiConsole.WriteLine();
        if (useColor)
        {
            var summaryColor = errorCount > 0 ? "red" : (warningCount > 0 ? "yellow" : "green");
            AnsiConsole.MarkupLine($"[{summaryColor}]Summary: {warningCount} warning(s), {errorCount} error(s)[/]");
        }
        else
        {
            Console.WriteLine($"Summary: {warningCount} warning(s), {errorCount} error(s)");
        }
    }

    /// <summary>
    /// Displays a single check result with appropriate formatting.
    /// </summary>
    private void DisplayCheck(DiagnosticResult result, int indent, bool useColor)
    {
        var prefix = new string(' ', indent);
        var (symbol, color) = result.Level switch
        {
            DiagnosticLevels.OK => ("V", "green"),      // checkmark
            DiagnosticLevels.Warning => ("!", "yellow"), // warning
            DiagnosticLevels.Error => ("X", "red"),      // error
            _ => ("?", "grey")
        };

        if (useColor)
        {
            AnsiConsole.MarkupLine($"{prefix}[{color}]{symbol}[/] {Markup.Escape(result.Component)}: {Markup.Escape(result.Message)}");
        }
        else
        {
            Console.WriteLine($"{prefix}{symbol} {result.Component}: {result.Message}");
        }
    }

    /// <summary>
    /// Gets the default config path.
    /// </summary>
    private static string GetDefaultConfigPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Constants.ConfigDefaults.DefaultConfigDirName,
            Constants.ConfigDefaults.DefaultConfigFileName);
    }
}
