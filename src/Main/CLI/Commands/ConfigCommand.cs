// Copyright (c) Microsoft. All rights reserved.
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using KernelMemory.Main.CLI.Models;
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
}

/// <summary>
/// Command to query configuration.
/// </summary>
public class ConfigCommand : BaseCommand<ConfigCommandSettings>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigCommand"/> class.
    /// </summary>
    /// <param name="config">Application configuration (injected by DI).</param>
    public ConfigCommand(KernelMemory.Core.Config.AppConfig config) : base(config)
    {
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
                // Default: show entire configuration with all nodes
                var config = this.Config;
                output = new
                {
                    Nodes = config.Nodes.Select(kvp => new NodeDetailsDto
                    {
                        NodeId = kvp.Key,
                        Access = kvp.Value.Access.ToString(),
                        ContentIndex = new ContentIndexConfigDto
                        {
                            Type = kvp.Value.ContentIndex.Type.ToString(),
                            Path = kvp.Value.ContentIndex is KernelMemory.Core.Config.ContentIndex.SqliteContentIndexConfig sqlite
                                ? sqlite.Path
                                : null
                        },
                        FileStorage = kvp.Value.FileStorage != null ? new StorageConfigDto
                        {
                            Type = kvp.Value.FileStorage.Type.ToString()
                        } : null,
                        RepoStorage = kvp.Value.RepoStorage != null ? new StorageConfigDto
                        {
                            Type = kvp.Value.RepoStorage.Type.ToString()
                        } : null,
                        SearchIndexes = kvp.Value.SearchIndexes.Select(si => new SearchIndexDto
                        {
                            Type = si.Type.ToString()
                        }).ToList()
                    }).ToList(),
                    Cache = new CacheInfoDto
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
                    }
                };
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
}
