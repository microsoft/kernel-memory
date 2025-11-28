// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json;
using KernelMemory.Core.Storage.Models;
using Spectre.Console;

namespace KernelMemory.Main.CLI.OutputFormatters;

/// <summary>
/// Formats output in human-readable format with colors (using Spectre.Console).
/// </summary>
public class HumanOutputFormatter : IOutputFormatter
{
    private readonly bool _useColors;
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public string Verbosity { get; }

    public HumanOutputFormatter(string verbosity, bool useColors)
    {
        this.Verbosity = verbosity;
        this._useColors = useColors;

        // Disable colors if requested
        if (!useColors)
        {
            AnsiConsole.Profile.Capabilities.ColorSystem = ColorSystem.NoColors;
        }
    }

    public void Format(object data)
    {
        if (this.Verbosity.Equals("silent", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        switch (data)
        {
            case ContentDto content:
                this.FormatContent(content);
                break;
            case string str:
                AnsiConsole.WriteLine(str);
                break;
            default:
                // For unknown types (like DTO objects), format as indented JSON
                // to avoid leaking internal type names
                this.FormatAsJson(data);
                break;
        }
    }

    private void FormatAsJson(object data)
    {
        var json = JsonSerializer.Serialize(data, s_jsonOptions);
        AnsiConsole.WriteLine(json);
    }

    public void FormatError(string errorMessage)
    {
        if (this._useColors)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(errorMessage)}");
        }
        else
        {
            Console.Error.WriteLine($"Error: {errorMessage}");
        }
    }

    public void FormatList<T>(IEnumerable<T> items, long totalCount, int skip, int take)
    {
        if (this.Verbosity.Equals("silent", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var itemsList = items.ToList();

        if (typeof(T) == typeof(ContentDto))
        {
            this.FormatContentList(itemsList.Cast<ContentDto>(), totalCount, skip, take);
        }
        else if (typeof(T) == typeof(string))
        {
            this.FormatStringList(itemsList.Cast<string>(), totalCount, skip, take);
        }
        else
        {
            this.FormatGenericList(itemsList, totalCount, skip, take);
        }
    }

    private void FormatContent(ContentDto content)
    {
        var isQuiet = this.Verbosity.Equals("quiet", StringComparison.OrdinalIgnoreCase);
        var isVerbose = this.Verbosity.Equals("verbose", StringComparison.OrdinalIgnoreCase);

        if (isQuiet)
        {
            // Quiet mode: just the ID
            AnsiConsole.WriteLine(content.Id);
            return;
        }

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("Property");
        table.AddColumn("Value");

        table.AddRow("[yellow]ID[/]", Markup.Escape(content.Id));
        table.AddRow("[yellow]MimeType[/]", Markup.Escape(content.MimeType));
        table.AddRow("[yellow]Size[/]", $"{content.ByteSize} bytes");

        // Truncate content unless verbose
        var displayContent = content.Content;
        if (!isVerbose && displayContent.Length > Constants.MaxContentDisplayLength)
        {
            displayContent = string.Concat(displayContent.AsSpan(0, Constants.MaxContentDisplayLength), "...");
        }
        table.AddRow("[yellow]Content[/]", Markup.Escape(displayContent));

        if (!string.IsNullOrEmpty(content.Title))
        {
            table.AddRow("[yellow]Title[/]", Markup.Escape(content.Title));
        }

        if (!string.IsNullOrEmpty(content.Description))
        {
            table.AddRow("[yellow]Description[/]", Markup.Escape(content.Description));
        }

        if (content.Tags.Length > 0)
        {
            table.AddRow("[yellow]Tags[/]", Markup.Escape(string.Join(", ", content.Tags)));
        }

        if (isVerbose)
        {
            table.AddRow("[yellow]ContentCreatedAt[/]", content.ContentCreatedAt.ToString("O"));
            table.AddRow("[yellow]RecordCreatedAt[/]", content.RecordCreatedAt.ToString("O"));
            table.AddRow("[yellow]RecordUpdatedAt[/]", content.RecordUpdatedAt.ToString("O"));

            if (content.Metadata.Count > 0)
            {
                var metadataStr = string.Join(", ", content.Metadata.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                table.AddRow("[yellow]Metadata[/]", Markup.Escape(metadataStr));
            }
        }

        AnsiConsole.Write(table);
    }

    private void FormatContentList(IEnumerable<ContentDto> contents, long totalCount, int skip, int take)
    {
        var isQuiet = this.Verbosity.Equals("quiet", StringComparison.OrdinalIgnoreCase);
        var contentsList = contents.ToList();

        // Check if list is empty
        if (contentsList.Count == 0)
        {
            if (this._useColors)
            {
                AnsiConsole.MarkupLine("[dim]No content found[/]");
            }
            else
            {
                AnsiConsole.WriteLine("No content found");
            }
            return;
        }

        if (isQuiet)
        {
            // Quiet mode: just IDs
            foreach (var content in contentsList)
            {
                AnsiConsole.WriteLine(content.Id);
            }
            return;
        }

        // Show pagination info
        AnsiConsole.MarkupLine($"[cyan]Showing {contentsList.Count} of {totalCount} items (skip: {skip})[/]");
        AnsiConsole.WriteLine();

        // Create table
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[yellow]ID[/]");
        table.AddColumn("[yellow]MimeType[/]");
        table.AddColumn("[yellow]Size[/]");
        table.AddColumn("[yellow]Content Preview[/]");
        table.AddColumn("[yellow]Created[/]");

        foreach (var content in contentsList)
        {
            var preview = content.Content.Length > 50
                ? string.Concat(content.Content.AsSpan(0, 50), "...")
                : content.Content;

            table.AddRow(
                Markup.Escape(content.Id),
                Markup.Escape(content.MimeType),
                $"{content.ByteSize}",
                Markup.Escape(preview),
                content.RecordCreatedAt.ToString("yyyy-MM-dd HH:mm")
            );
        }

        AnsiConsole.Write(table);
    }

    private void FormatStringList(IEnumerable<string> items, long totalCount, int skip, int take)
    {
        var itemsList = items.ToList();

        // Check if list is empty
        if (itemsList.Count == 0)
        {
            if (this._useColors)
            {
                AnsiConsole.MarkupLine("[dim]No items found[/]");
            }
            else
            {
                AnsiConsole.WriteLine("No items found");
            }
            return;
        }

        if (this.Verbosity.Equals("quiet", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var item in itemsList)
            {
                AnsiConsole.WriteLine(item);
            }
            return;
        }

        AnsiConsole.MarkupLine($"[cyan]Showing {itemsList.Count} of {totalCount} items[/]");
        AnsiConsole.WriteLine();

        foreach (var item in itemsList)
        {
            AnsiConsole.MarkupLine($"  [green]•[/] {Markup.Escape(item)}");
        }
    }

    private void FormatGenericList<T>(IEnumerable<T> items, long totalCount, int skip, int take)
    {
        var itemsList = items.ToList();

        // Check if list is empty
        if (itemsList.Count == 0)
        {
            if (this._useColors)
            {
                AnsiConsole.MarkupLine("[dim]No items found[/]");
            }
            else
            {
                AnsiConsole.WriteLine("No items found");
            }
            return;
        }

        AnsiConsole.MarkupLine($"[cyan]Showing {itemsList.Count} of {totalCount} items[/]");
        AnsiConsole.WriteLine();

        foreach (var item in itemsList)
        {
            AnsiConsole.WriteLine(item?.ToString() ?? string.Empty);
        }
    }
}
