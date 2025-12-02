// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using KernelMemory.Core.Config;
using KernelMemory.Core.Storage.Models;
using KernelMemory.Main.CLI.OutputFormatters;
using Spectre.Console;
using Spectre.Console.Cli;

namespace KernelMemory.Main.CLI.Commands;

/// <summary>
/// Settings for the upsert command.
/// </summary>
public class UpsertCommandSettings : GlobalOptions
{
    [CommandArgument(0, "<content>")]
    [Description("Content to upload")]
    public string Content { get; init; } = string.Empty;

    [CommandOption("--id")]
    [Description("Content ID (generated if not provided)")]
    public string? Id { get; init; }

    [CommandOption("--mime-type")]
    [Description("MIME type (default: text/plain)")]
    [DefaultValue("text/plain")]
    public string MimeType { get; init; } = "text/plain";

    [CommandOption("--title")]
    [Description("Optional title")]
    public string? Title { get; init; }

    [CommandOption("--description")]
    [Description("Optional description")]
    public string? Description { get; init; }

    [CommandOption("--tags")]
    [Description("Optional tags (comma-separated)")]
    public string? Tags { get; init; }

    public override ValidationResult Validate()
    {
        var baseResult = base.Validate();
        if (!baseResult.Successful)
        {
            return baseResult;
        }

        if (string.IsNullOrWhiteSpace(this.Content))
        {
            return ValidationResult.Error("Content cannot be empty");
        }

        return ValidationResult.Success();
    }
}

/// <summary>
/// Command to upsert (create or update) content.
/// </summary>
public class UpsertCommand : BaseCommand<UpsertCommandSettings>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpsertCommand"/> class.
    /// </summary>
    /// <param name="config">Application configuration (injected by DI).</param>
    public UpsertCommand(AppConfig config) : base(config)
    {
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        UpsertCommandSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            var (config, node, formatter) = this.Initialize(settings);
            using var service = this.CreateContentService(node);

            // Parse tags if provided
            var tags = string.IsNullOrWhiteSpace(settings.Tags)
                ? Array.Empty<string>()
                : settings.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // Create upsert request
            var request = new UpsertRequest
            {
                Id = settings.Id ?? string.Empty,
                Content = settings.Content,
                MimeType = settings.MimeType,
                Title = settings.Title ?? string.Empty,
                Description = settings.Description ?? string.Empty,
                Tags = tags,
                Metadata = new Dictionary<string, string>()
            };

            // Perform upsert
            var result = await service.UpsertAsync(request, CancellationToken.None).ConfigureAwait(false);

            // Output result based on verbosity
            if (settings.Verbosity.Equals("quiet", StringComparison.OrdinalIgnoreCase))
            {
                formatter.Format(result.Id);
            }
            else
            {
                formatter.Format(new
                {
                    id = result.Id,
                    node = node.Id,
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
