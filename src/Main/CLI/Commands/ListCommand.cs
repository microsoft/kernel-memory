// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using KernelMemory.Core;
using KernelMemory.Core.Config;
using KernelMemory.Core.Storage.Models;
using KernelMemory.Main.CLI.Exceptions;
using KernelMemory.Main.CLI.OutputFormatters;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace KernelMemory.Main.CLI.Commands;

/// <summary>
/// Settings for the list command.
/// </summary>
public class ListCommandSettings : GlobalOptions
{
    [CommandOption("--skip")]
    [Description("Number of items to skip (default: 0)")]
    [DefaultValue(0)]
    public int Skip { get; init; }

    [CommandOption("--take")]
    [Description("Number of items to take (default: 20)")]
    [DefaultValue(20)]
    public int Take { get; init; } = Constants.App.DefaultPageSize;

    public override ValidationResult Validate()
    {
        var baseResult = base.Validate();
        if (!baseResult.Successful)
        {
            return baseResult;
        }

        if (this.Skip < 0)
        {
            return ValidationResult.Error("Skip must be >= 0");
        }

        if (this.Take <= 0)
        {
            return ValidationResult.Error("Take must be > 0");
        }

        return ValidationResult.Success();
    }
}

/// <summary>
/// Command to list all content with pagination.
/// </summary>
public class ListCommand : BaseCommand<ListCommandSettings>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListCommand"/> class.
    /// </summary>
    /// <param name="config">Application configuration (injected by DI).</param>
    /// <param name="loggerFactory">Logger factory for creating loggers (injected by DI).</param>
    public ListCommand(AppConfig config, ILoggerFactory loggerFactory) : base(config, loggerFactory)
    {
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ListCommandSettings settings,
        CancellationToken cancellationToken)
    {
        var (config, node, formatter) = this.Initialize(settings);

        try
        {
            using var service = this.CreateContentService(node, readonlyMode: true);

            // Get total count
            var totalCount = await service.CountAsync(CancellationToken.None).ConfigureAwait(false);

            // Get page of items
            var items = await service.ListAsync(settings.Skip, settings.Take, CancellationToken.None).ConfigureAwait(false);

            // Wrap items with node information
            var itemsWithNode = items.Select(item =>
                ContentDtoWithNode.FromContentDto(item, node.Id));

            // Format list with pagination info
            formatter.FormatList(itemsWithNode, totalCount, settings.Skip, settings.Take);

            return Constants.App.ExitCodeSuccess;
        }
        catch (DatabaseNotFoundException)
        {
            // First-run scenario: no database exists yet (expected state)
            this.ShowFirstRunMessage(settings, node.Id);
            return Constants.App.ExitCodeSuccess; // Not a user error
        }
        catch (Exception ex)
        {
            return this.HandleError(ex, formatter);
        }
    }

    /// <summary>
    /// Shows a friendly first-run message when no database exists yet.
    /// </summary>
    /// <param name="settings">Command settings for output format.</param>
    /// <param name="nodeId">The node ID being listed.</param>
    private void ShowFirstRunMessage(ListCommandSettings settings, string nodeId)
    {
        var formatter = OutputFormatterFactory.Create(settings);

        // For JSON/YAML, return empty list (valid, parseable output)
        if (!settings.Format.Equals("human", StringComparison.OrdinalIgnoreCase))
        {
            formatter.FormatList(Array.Empty<ContentDto>(), 0, 0, settings.Take);
            return;
        }

        // Human format: friendly welcome message
        // Include --node parameter if not using the first (default) node
        var isDefaultNode = nodeId == this.Config.Nodes.Keys.First();
        var nodeParam = isDefaultNode ? "" : $" --node {nodeId}";

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold green]Welcome to Kernel Memory! 🚀[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]No content found in node '{nodeId}' yet.[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]To get started:[/]");
        AnsiConsole.MarkupLine($"  [cyan]km put \"Your content here\"{nodeParam}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Example:[/]");
        AnsiConsole.MarkupLine($"  [cyan]km put \"Hello, world!\" --id greeting{nodeParam}[/]");
        AnsiConsole.WriteLine();
    }
}
