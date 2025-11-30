// Copyright (c) Microsoft. All rights reserved.
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using KernelMemory.Main.CLI.Infrastructure;
using KernelMemory.Main.CLI.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace KernelMemory.Main.CLI.Commands;

/// <summary>
/// Settings for the config command.
/// </summary>
public class ConfigCommandSettings : GlobalOptions
{
    [CommandOption("--show-nodes")]
    [Description("Show all nodes configuration")]
    public bool ShowNodes { get; init; }

    [CommandOption("--show-cache")]
    [Description("Show cache configuration")]
    public bool ShowCache { get; init; }

    [CommandOption("--create")]
    [Description("Create configuration file on disk")]
    public bool Create { get; init; }
}

/// <summary>
/// Command to query configuration.
/// </summary>
public class ConfigCommand : BaseCommand<ConfigCommandSettings>
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ConfigPathService _configPathService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigCommand"/> class.
    /// </summary>
    /// <param name="config">Application configuration (injected by DI).</param>
    /// <param name="configPathService">Service providing the config file path (injected by DI).</param>
    public ConfigCommand(
        KernelMemory.Core.Config.AppConfig config,
        ConfigPathService configPathService) : base(config)
    {
        this._configPathService = configPathService ?? throw new ArgumentNullException(nameof(configPathService));
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Top-level command handler must catch all exceptions to return appropriate exit codes and error messages")]
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ConfigCommandSettings settings)
    {
        try
        {
            // ConfigCommand doesn't need node selection - it queries the entire configuration
            // So we skip Initialize() and just use the injected config directly
            var formatter = CLI.OutputFormatters.OutputFormatterFactory.Create(settings);
            var configPath = this._configPathService.Path;
            var configFileExists = File.Exists(configPath);

            // Handle --create flag
            if (settings.Create)
            {
                return this.HandleCreateConfig(configPath, configFileExists, formatter);
            }

            // Show warning if using default config without file
            if (!configFileExists && !settings.NoColor)
            {
                AnsiConsole.MarkupLine("[yellow]Warning: Using default configuration. Config file does not exist: {0}[/]", Markup.Escape(configPath));
                AnsiConsole.MarkupLine("[yellow]Run 'km config --create' to save the current configuration to disk.[/]");
                AnsiConsole.WriteLine();
            }

            // Determine what to show
            object output;

            if (settings.ShowNodes)
            {
                // Show all nodes summary
                output = this.Config.Nodes.Select(kvp => new NodeSummaryDto
                {
                    Id = kvp.Key,
                    Access = kvp.Value.Access.ToString(),
                    ContentIndex = kvp.Value.ContentIndex.Type.ToString(),
                    HasFileStorage = kvp.Value.FileStorage != null,
                    HasRepoStorage = kvp.Value.RepoStorage != null,
                    SearchIndexCount = kvp.Value.SearchIndexes.Count
                }).ToList();
            }
            else if (settings.ShowCache)
            {
                // Show cache configuration
                var config = this.Config;
                output = new CacheInfoDto
                {
                    EmbeddingsCache = config.EmbeddingsCache != null ? new CacheConfigDto
                    {
                        Type = config.EmbeddingsCache.Type.ToString(),
                        Path = config.EmbeddingsCache.Path
                    } : null,
                    LlmCache = config.LLMCache != null ? new CacheConfigDto
                    {
                        Type = config.LLMCache.Type.ToString(),
                        Path = config.LLMCache.Path
                    } : null
                };
            }
            else
            {
                // Default: show the actual AppConfig structure (not DTOs)
                // This allows users to copy/paste the output into their config file
                output = this.Config;
            }

            formatter.Format(output);

            return Constants.ExitCodeSuccess;
        }
        catch (Exception ex)
        {
            var formatter = CLI.OutputFormatters.OutputFormatterFactory.Create(settings);
            return this.HandleError(ex, formatter);
        }
    }

    /// <summary>
    /// Handles the --create flag to write the configuration to disk.
    /// </summary>
    /// <param name="configPath">Path to write the config file.</param>
    /// <param name="configFileExists">Whether the config file already exists.</param>
    /// <param name="formatter">Output formatter for messages.</param>
    /// <returns>Exit code.</returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Config creation must catch all exceptions to return appropriate exit codes")]
    private int HandleCreateConfig(string configPath, bool configFileExists, CLI.OutputFormatters.IOutputFormatter formatter)
    {
        try
        {
            // Warn if file already exists
            if (configFileExists)
            {
                formatter.FormatError($"Configuration file already exists: {configPath}");
                return Constants.ExitCodeUserError;
            }

            // Ensure directory exists
            var configDir = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            // Serialize config with nice formatting
            var json = JsonSerializer.Serialize(this.Config, s_jsonOptions);

            // Write to file
            File.WriteAllText(configPath, json);

            formatter.Format(new { Message = $"Configuration file created: {configPath}" });

            return Constants.ExitCodeSuccess;
        }
        catch (Exception ex)
        {
            formatter.FormatError($"Failed to create configuration file: {ex.Message}");
            return Constants.ExitCodeSystemError;
        }
    }
}
