// Copyright (c) Microsoft. All rights reserved.
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Top-level command handler must catch all exceptions to return appropriate exit codes and error messages")]
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ListCommandSettings settings)
    {
        try
        {
            var (config, node, formatter) = await this.InitializeAsync(settings).ConfigureAwait(false);
            var service = this.CreateContentService(node);

            // Get total count
            var totalCount = await service.CountAsync(CancellationToken.None).ConfigureAwait(false);

            // Get page of items
            var items = await service.ListAsync(settings.Skip, settings.Take, CancellationToken.None).ConfigureAwait(false);

            // Format list with pagination info
            formatter.FormatList(items, totalCount, settings.Skip, settings.Take);

            return Constants.ExitCodeSuccess;
        }
        catch (Exception ex)
        {
            var formatter = CLI.OutputFormatters.OutputFormatterFactory.Create(settings);
            return this.HandleError(ex, formatter);
        }
    }
}
