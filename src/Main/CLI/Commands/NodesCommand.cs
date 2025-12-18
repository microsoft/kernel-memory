// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Core;
using KernelMemory.Core.Config;
using KernelMemory.Main.CLI.OutputFormatters;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

namespace KernelMemory.Main.CLI.Commands;

/// <summary>
/// Settings for the nodes command (uses global options only).
/// </summary>
public class NodesCommandSettings : GlobalOptions
{
}

/// <summary>
/// Command to list all configured nodes.
/// </summary>
public class NodesCommand : BaseCommand<NodesCommandSettings>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodesCommand"/> class.
    /// </summary>
    /// <param name="config">Application configuration (injected by DI).</param>
    /// <param name="loggerFactory">Logger factory for creating loggers (injected by DI).</param>
    public NodesCommand(AppConfig config, ILoggerFactory loggerFactory) : base(config, loggerFactory)
    {
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        NodesCommandSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            var (config, node, formatter) = this.Initialize(settings);

            // Get all node IDs
            var nodeIds = config.Nodes.Keys.ToList();
            var totalCount = nodeIds.Count;

            // Format as list
            formatter.FormatList(nodeIds, totalCount, 0, totalCount);

            return Constants.App.ExitCodeSuccess;
        }
        catch (Exception ex)
        {
            var formatter = OutputFormatterFactory.Create(settings);
            return this.HandleError(ex, formatter);
        }
    }
}
