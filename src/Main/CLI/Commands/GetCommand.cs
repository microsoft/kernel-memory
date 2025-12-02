// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using KernelMemory.Core.Config;
using KernelMemory.Core.Storage.Models;
using KernelMemory.Main.CLI.Exceptions;
using KernelMemory.Main.CLI.OutputFormatters;
using Spectre.Console;
using Spectre.Console.Cli;

namespace KernelMemory.Main.CLI.Commands;

/// <summary>
/// Settings for the get command.
/// </summary>
public class GetCommandSettings : GlobalOptions
{
    [CommandArgument(0, "<id>")]
    [Description("Content ID to retrieve")]
    public string Id { get; init; } = string.Empty;

    [CommandOption("--full")]
    [Description("Show all internal details")]
    public bool ShowFull { get; init; }

    public override ValidationResult Validate()
    {
        var baseResult = base.Validate();
        if (!baseResult.Successful)
        {
            return baseResult;
        }

        if (string.IsNullOrWhiteSpace(this.Id))
        {
            return ValidationResult.Error("ID cannot be empty");
        }

        return ValidationResult.Success();
    }
}

/// <summary>
/// Command to get content by ID.
/// </summary>
public class GetCommand : BaseCommand<GetCommandSettings>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetCommand"/> class.
    /// </summary>
    /// <param name="config">Application configuration (injected by DI).</param>
    public GetCommand(AppConfig config) : base(config)
    {
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        GetCommandSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            var (config, node, formatter) = this.Initialize(settings);
            using var service = this.CreateContentService(node, readonlyMode: true);

            var result = await service.GetAsync(settings.Id, CancellationToken.None).ConfigureAwait(false);

            if (result == null)
            {
                formatter.FormatError($"Content with ID '{settings.Id}' not found");
                return Constants.ExitCodeUserError;
            }

            // Wrap result with node information
            var response = ContentDtoWithNode.FromContentDto(result, node.Id);

            formatter.Format(response);

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
            var formatter = OutputFormatterFactory.Create(settings);
            return this.HandleError(ex, formatter);
        }
    }

    /// <summary>
    /// Shows a friendly first-run message when no database exists yet.
    /// </summary>
    /// <param name="settings">Command settings for output format.</param>
    private void ShowFirstRunMessage(GetCommandSettings settings)
    {
        var formatter = OutputFormatterFactory.Create(settings);

        // For JSON/YAML, return null (valid, parseable output)
        if (!settings.Format.Equals("human", StringComparison.OrdinalIgnoreCase))
        {
            formatter.Format(null!);
            return;
        }

        // Human format: friendly message with context
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[yellow]Content with ID '{settings.Id}' not found.[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]No content database exists yet. This is your first run.[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Create content first:[/]");
        AnsiConsole.MarkupLine($"  [cyan]km put \"Your content here\" --id {settings.Id}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Or list available content:[/]");
        AnsiConsole.MarkupLine("  [cyan]km list[/]");
        AnsiConsole.WriteLine();
    }
}
