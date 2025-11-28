// Copyright (c) Microsoft. All rights reserved.
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
    public GetCommand(KernelMemory.Core.Config.AppConfig config) : base(config)
    {
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Top-level command handler must catch all exceptions to return appropriate exit codes and error messages")]
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        GetCommandSettings settings)
    {
        try
        {
            var (config, node, formatter) = this.Initialize(settings);
            var service = this.CreateContentService(node, readonlyMode: true);

            var result = await service.GetAsync(settings.Id, CancellationToken.None).ConfigureAwait(false);

            if (result == null)
            {
                formatter.FormatError($"Content with ID '{settings.Id}' not found");
                return Constants.ExitCodeUserError;
            }

            // If --full flag is set, ensure verbose mode for human formatter
            // For JSON/YAML, all fields are always included
            formatter.Format(result);

            return Constants.ExitCodeSuccess;
        }
        catch (Exception ex)
        {
            var formatter = CLI.OutputFormatters.OutputFormatterFactory.Create(settings);
            return this.HandleError(ex, formatter);
        }
    }
}
