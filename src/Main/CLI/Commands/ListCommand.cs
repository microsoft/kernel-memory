// Copyright (c) Microsoft. All rights reserved.
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using KernelMemory.Main.CLI.Exceptions;
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
    public int Take { get; init; } = Constants.DefaultPageSize;

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
    public ListCommand(KernelMemory.Core.Config.AppConfig config) : base(config)
    {
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Top-level command handler must catch all exceptions to return appropriate exit codes and error messages")]
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ListCommandSettings settings)
    {
        try
        {
            var (config, node, formatter) = this.Initialize(settings);
            var service = this.CreateContentService(node, readonlyMode: true);

            // Get total count
            var totalCount = await service.CountAsync(CancellationToken.None).ConfigureAwait(false);

            // Get page of items
            var items = await service.ListAsync(settings.Skip, settings.Take, CancellationToken.None).ConfigureAwait(false);

            // Wrap items with node information
            var itemsWithNode = items.Select(item => new
            {
                id = item.Id,
                node = node.Id,
                content = item.Content,
                mimeType = item.MimeType,
                byteSize = item.ByteSize,
                contentCreatedAt = item.ContentCreatedAt,
                recordCreatedAt = item.RecordCreatedAt,
                recordUpdatedAt = item.RecordUpdatedAt,
                title = item.Title,
                description = item.Description,
                tags = item.Tags,
                metadata = item.Metadata
            });

            // Format list with pagination info
            formatter.FormatList(itemsWithNode, totalCount, settings.Skip, settings.Take);

            return Constants.ExitCodeSuccess;
        }
        catch (DatabaseNotFoundException)
        {
            // First-run scenario: no database exists yet (expected state)
            this.ShowFirstRunMessage(settings);
            return Constants.ExitCodeSuccess; // Not a user error
        }
        catch (Exception ex)
        {
            var formatter = CLI.OutputFormatters.OutputFormatterFactory.Create(settings);
            return this.HandleError(ex, formatter);
        }
    }

    /// <summary>
    /// Shows a friendly first-run message when no database exists yet.
    /// </summary>
    /// <param name="settings">Command settings for output format.</param>
    private void ShowFirstRunMessage(ListCommandSettings settings)
    {
        var formatter = CLI.OutputFormatters.OutputFormatterFactory.Create(settings);

        // For JSON/YAML, return empty list (valid, parseable output)
        if (!settings.Format.Equals("human", StringComparison.OrdinalIgnoreCase))
        {
            formatter.FormatList(Array.Empty<Core.Storage.Models.ContentDto>(), 0, 0, settings.Take);
            return;
        }

        // Human format: friendly welcome message
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold green]Welcome to Kernel Memory! 🚀[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]No content found yet. This is your first run.[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]To get started:[/]");
        AnsiConsole.MarkupLine("  [cyan]km put \"Your content here\"[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Example:[/]");
        AnsiConsole.MarkupLine("  [cyan]km put \"Hello, world!\" --id greeting[/]");
        AnsiConsole.WriteLine();
    }
}
