// Copyright (c) Microsoft. All rights reserved.
using System.Diagnostics.CodeAnalysis;
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
    public NodesCommand(KernelMemory.Core.Config.AppConfig config) : base(config)
    {
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Top-level command handler must catch all exceptions to return appropriate exit codes and error messages")]
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        NodesCommandSettings settings)
    {
        try
        {
            var (config, node, formatter) = this.Initialize(settings);

            // Get all node IDs
            var nodeIds = config.Nodes.Keys.ToList();
            var totalCount = nodeIds.Count;

            // Format as list
            formatter.FormatList(nodeIds, totalCount, 0, totalCount);

            return Constants.ExitCodeSuccess;
        }
        catch (Exception ex)
        {
            var formatter = CLI.OutputFormatters.OutputFormatterFactory.Create(settings);
            return this.HandleError(ex, formatter);
        }
    }
}
