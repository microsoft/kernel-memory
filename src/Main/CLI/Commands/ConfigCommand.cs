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
    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Top-level command handler must catch all exceptions to return appropriate exit codes and error messages")]
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ConfigCommandSettings settings)
    {
        try
        {
            var (config, node, formatter) = await this.InitializeAsync(settings).ConfigureAwait(false);

            // Determine what to show
            object output;

            if (settings.ShowNodes)
            {
                // Show all nodes
                output = config.Nodes.Select(kvp => new NodeSummaryDto
                {
                    Id = kvp.Key,
                    Access = kvp.Value.Access.ToString(),
                    ContentIndex = kvp.Value.ContentIndex.GetType().Name,
                    HasFileStorage = kvp.Value.FileStorage != null,
                    HasRepoStorage = kvp.Value.RepoStorage != null,
                    SearchIndexCount = kvp.Value.SearchIndexes.Count
                }).ToList();
            }
            else if (settings.ShowCache)
            {
                // Show cache configuration
                output = new CacheInfoDto
                {
                    EmbeddingsCache = config.EmbeddingsCache != null ? new CacheConfigDto
                    {
                        Type = config.EmbeddingsCache.GetType().Name,
                        Path = config.EmbeddingsCache.Path
                    } : null,
                    LlmCache = config.LLMCache != null ? new CacheConfigDto
                    {
                        Type = config.LLMCache.GetType().Name,
                        Path = config.LLMCache.Path
                    } : null
                };
            }
            else
            {
                // Default: show current node details
                output = new NodeDetailsDto
                {
                    NodeId = node.Id,
                    Access = node.Access.ToString(),
                    ContentIndex = new ContentIndexConfigDto
                    {
                        Type = node.ContentIndex.GetType().Name,
                        Path = node.ContentIndex is KernelMemory.Core.Config.ContentIndex.SqliteContentIndexConfig sqlite
                            ? sqlite.Path
                            : null
                    },
                    FileStorage = node.FileStorage != null ? new StorageConfigDto
                    {
                        Type = node.FileStorage.GetType().Name
                    } : null,
                    RepoStorage = node.RepoStorage != null ? new StorageConfigDto
                    {
                        Type = node.RepoStorage.GetType().Name
                    } : null,
                    SearchIndexes = node.SearchIndexes.Select(si => new SearchIndexDto
                    {
                        Type = si.Type.ToString()
                    }).ToList()
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
