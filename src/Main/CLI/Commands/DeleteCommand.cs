// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using KernelMemory.Core.Config;
using KernelMemory.Main.CLI.OutputFormatters;
using Spectre.Console;
using Spectre.Console.Cli;

namespace KernelMemory.Main.CLI.Commands;

/// <summary>
/// Settings for the delete command.
/// </summary>
public class DeleteCommandSettings : GlobalOptions
{
    [CommandArgument(0, "<id>")]
    [Description("Content ID to delete")]
    public string Id { get; init; } = string.Empty;

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
/// Command to delete content by ID.
/// </summary>
public class DeleteCommand : BaseCommand<DeleteCommandSettings>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteCommand"/> class.
    /// </summary>
    /// <param name="config">Application configuration (injected by DI).</param>
    public DeleteCommand(AppConfig config) : base(config)
    {
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DeleteCommandSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            var (config, node, formatter) = this.Initialize(settings);
            using var service = this.CreateContentService(node);

            // Delete is idempotent - no error if not found
            var result = await service.DeleteAsync(settings.Id, CancellationToken.None).ConfigureAwait(false);

            // Output result based on verbosity
            if (settings.Verbosity.Equals("quiet", StringComparison.OrdinalIgnoreCase))
            {
                formatter.Format(result.Id);
            }
            else if (!settings.Verbosity.Equals("silent", StringComparison.OrdinalIgnoreCase))
            {
                formatter.Format(new
                {
                    id = result.Id,
                    completed = result.Completed,
                    queued = result.Queued,
                    error = string.IsNullOrEmpty(result.Error) ? null : result.Error
                });
            }

            return Constants.ExitCodeSuccess;
        }
        catch (Exception ex)
        {
            var formatter = OutputFormatterFactory.Create(settings);
            return this.HandleError(ex, formatter);
        }
    }
}
