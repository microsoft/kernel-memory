// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace KernelMemory.Main.CLI.Commands;

/// <summary>
/// Command to display examples for all CLI commands.
/// </summary>
public sealed class ExamplesCommand : Command<ExamplesCommand.Settings>
{
    /// <summary>
    /// Settings for the examples command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--command")]
        [Description("Show examples for a specific command (e.g., search, put, get)")]
        public string? Command { get; init; }
    }

    /// <inheritdoc />
    public override int Execute(CommandContext context, Settings settings)
    {
        if (!string.IsNullOrEmpty(settings.Command))
        {
            this.ShowCommandExamples(settings.Command);
        }
        else
        {
            this.ShowAllExamples();
        }

        return 0;
    }

    private void ShowAllExamples()
    {
        AnsiConsole.Write(new Rule("[bold cyan]📚 Kernel Memory - Quick Start Guide[/]").LeftJustified());
        AnsiConsole.WriteLine();

        this.ShowPutExamples();
        this.ShowSearchExamples();
        this.ShowListExamples();
        this.ShowGetExamples();
        this.ShowDeleteExamples();
        this.ShowNodesExamples();
        this.ShowConfigExamples();
        this.ShowAdvancedExamples();
    }

    private void ShowCommandExamples(string command)
    {
        var normalizedCommand = command.ToLowerInvariant();

        AnsiConsole.Write(new Rule($"[bold cyan]📚 Quick ideas for '{normalizedCommand}' command[/]").LeftJustified());
        AnsiConsole.WriteLine();

        switch (normalizedCommand)
        {
            case "search":
                this.ShowSearchExamples();
                break;
            case "put":
            case "upsert":
                this.ShowPutExamples();
                break;
            case "get":
                this.ShowGetExamples();
                break;
            case "list":
                this.ShowListExamples();
                break;
            case "delete":
                this.ShowDeleteExamples();
                break;
            case "nodes":
                this.ShowNodesExamples();
                break;
            case "config":
                this.ShowConfigExamples();
                break;
            case "advanced":
                this.ShowAdvancedExamples();
                break;
            default:
                AnsiConsole.MarkupLine($"[red]Unknown command: {command}[/]");
                AnsiConsole.MarkupLine("[dim]Available commands: search, put, get, list, delete, nodes, config, advanced[/]");
                break;
        }
    }

    private void ShowSearchExamples()
    {
        AnsiConsole.Write(new Rule("[yellow]🔍 SEARCH - Find your notes and memories[/]").LeftJustified());
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Simple keyword search[/]");
        AnsiConsole.MarkupLine("[cyan]km search \"doctor appointment\"[/]");
        AnsiConsole.MarkupLine("[dim]Find your medical appointment notes[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Search by topic[/]");
        AnsiConsole.MarkupLine("[cyan]km search \"title:lecture AND tags:exam\"[/]");
        AnsiConsole.MarkupLine("[dim]Find lecture notes related to upcoming exams[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Search with multiple conditions[/]");
        AnsiConsole.MarkupLine("[cyan]km search \"content:insurance AND (tags:health OR tags:auto)\"[/]");
        AnsiConsole.MarkupLine("[dim]Find health or auto insurance documents[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]See highlighted matches[/]");
        AnsiConsole.MarkupLine("[cyan]km search \"passport number\" --highlight --snippet[/]");
        AnsiConsole.MarkupLine("[dim]Show where your passport info appears in context[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Search specific collections[/]");
        AnsiConsole.MarkupLine("[cyan]km search \"project requirements\" --nodes work,personal[/]");
        AnsiConsole.MarkupLine("[dim]Search only your work and personal notes[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Browse through results[/]");
        AnsiConsole.MarkupLine("[cyan]km search \"meeting notes\" --limit 10 --offset 20[/]");
        AnsiConsole.MarkupLine("[dim]See results 21-30 of your meeting notes[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Find best matches only[/]");
        AnsiConsole.MarkupLine("[cyan]km search \"emergency contacts\" --min-relevance 0.7[/]");
        AnsiConsole.MarkupLine("[dim]Show only highly relevant emergency contact info[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Boolean operators - AND, OR[/]");
        AnsiConsole.MarkupLine("[cyan]km search \"docker AND kubernetes\"[/]");
        AnsiConsole.MarkupLine("[dim]Find documents containing both docker and kubernetes[/]");
        AnsiConsole.MarkupLine("[cyan]km search \"python OR javascript\"[/]");
        AnsiConsole.MarkupLine("[dim]Find documents with either python or javascript[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Complex queries with parentheses[/]");
        AnsiConsole.MarkupLine("[cyan]km search \"vacation AND (beach OR mountain)\"[/]");
        AnsiConsole.MarkupLine("[dim]Find vacation plans for beach or mountain trips[/]");
        AnsiConsole.MarkupLine("[cyan]km search \"title:api AND (content:rest OR content:graphql)\"[/]");
        AnsiConsole.MarkupLine("[dim]Find API docs about REST or GraphQL[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]MongoDB JSON query format[/]");
        AnsiConsole.MarkupLine($"[cyan]{Markup.Escape("km search '{\"content\": \"kubernetes\"}'")}[/]");
        AnsiConsole.MarkupLine("[dim]Alternative JSON syntax for simple queries[/]");
        AnsiConsole.MarkupLine($"[cyan]{Markup.Escape("km search '{\"$and\": [{\"title\": \"api\"}, {\"content\": \"rest\"}]}'")}[/]");
        AnsiConsole.MarkupLine("[dim]JSON format for complex boolean queries[/]");
        AnsiConsole.MarkupLine($"[cyan]{Markup.Escape("km search '{\"$text\": {\"$search\": \"full text query\"}}'")}[/]");
        AnsiConsole.MarkupLine("[dim]Full-text search across all fields[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]JSON format - escaping special characters[/]");
        AnsiConsole.MarkupLine($"[cyan]{Markup.Escape("km search '{\"content\": \"quotes: \\\"hello\\\"\"}'")}[/]");
        AnsiConsole.MarkupLine("[dim]Escape quotes in JSON with backslash[/]");
        AnsiConsole.MarkupLine($"[cyan]{Markup.Escape("km search '{\"content\": \"path\\\\to\\\\file\"}'")}[/]");
        AnsiConsole.MarkupLine("[dim]Escape backslashes in JSON (use double backslash)[/]");
        AnsiConsole.WriteLine();
    }

    private void ShowPutExamples()
    {
        AnsiConsole.Write(new Rule("[green]📤 SAVE - Store your thoughts and files[/]").LeftJustified());
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Quick note[/]");
        AnsiConsole.MarkupLine("[cyan]km put 'Call pediatrician for flu shot appointment'[/]");
        AnsiConsole.MarkupLine("[dim]Save a quick reminder or task[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]With your own ID[/]");
        AnsiConsole.MarkupLine("[cyan]km put 'Home insurance policy details' --id home-insurance[/]");
        AnsiConsole.MarkupLine("[dim]Easy to remember and find later[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Organize with tags[/]");
        AnsiConsole.MarkupLine("[cyan]km put 'Flight booking confirmation' --tags travel,important,2024[/]");
        AnsiConsole.MarkupLine("[dim]Tag for easy filtering and discovery[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Save a document[/]");
        AnsiConsole.MarkupLine("[cyan]km put school-schedule.pdf[/]");
        AnsiConsole.MarkupLine("[dim]Store any text file or PDF[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Save multiple files[/]");
        AnsiConsole.MarkupLine("[cyan]km put study-notes/*.md --tags semester1,finals[/]");
        AnsiConsole.MarkupLine("[dim]Import all your study notes at once[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Store in a specific collection[/]");
        AnsiConsole.MarkupLine("[cyan]km put 'Client project requirements' --nodes work --id project-alpha[/]");
        AnsiConsole.MarkupLine("[dim]Keep work and personal notes separate[/]");
        AnsiConsole.WriteLine();
    }

    private void ShowGetExamples()
    {
        AnsiConsole.Write(new Rule("[blue]📥 RETRIEVE - Get your saved content[/]").LeftJustified());
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Get by ID[/]");
        AnsiConsole.MarkupLine("[cyan]km get home-insurance[/]");
        AnsiConsole.MarkupLine("[dim]Retrieve your insurance policy details[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]See full content[/]");
        AnsiConsole.MarkupLine("[cyan]km get thesis-notes-2024 --full[/]");
        AnsiConsole.MarkupLine("[dim]Show everything, not just a preview[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Export as JSON[/]");
        AnsiConsole.MarkupLine("[cyan]km get client-meeting --format json[/]");
        AnsiConsole.MarkupLine("[dim]Export in a format you can process[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Get from specific collection[/]");
        AnsiConsole.MarkupLine("[cyan]km get budget-plan --nodes personal[/]");
        AnsiConsole.MarkupLine("[dim]Retrieve from your personal collection[/]");
        AnsiConsole.WriteLine();
    }

    private void ShowListExamples()
    {
        AnsiConsole.Write(new Rule("[purple]📋 BROWSE - See what you've saved[/]").LeftJustified());
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]See everything[/]");
        AnsiConsole.MarkupLine("[cyan]km list[/]");
        AnsiConsole.MarkupLine("[dim]Browse all your saved notes and files[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Browse page by page[/]");
        AnsiConsole.MarkupLine("[cyan]km list --skip 20 --take 10[/]");
        AnsiConsole.MarkupLine("[dim]View items 21-30[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]View specific collection[/]");
        AnsiConsole.MarkupLine("[cyan]km list --nodes personal[/]");
        AnsiConsole.MarkupLine("[dim]See only your personal notes[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Export list as JSON[/]");
        AnsiConsole.MarkupLine("[cyan]km list --format json[/]");
        AnsiConsole.MarkupLine("[dim]Get a structured list you can process[/]");
        AnsiConsole.WriteLine();
    }

    private void ShowDeleteExamples()
    {
        AnsiConsole.Write(new Rule("[red]🗑  REMOVE - Clean up old notes[/]").LeftJustified());
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Remove something[/]");
        AnsiConsole.MarkupLine("[cyan]km delete expired-coupon[/]");
        AnsiConsole.MarkupLine("[dim]Delete a note you no longer need[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Silent deletion[/]");
        AnsiConsole.MarkupLine("[cyan]km delete old-assignment-2023 --verbosity quiet[/]");
        AnsiConsole.MarkupLine("[dim]Delete without extra messages[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Delete from specific collection[/]");
        AnsiConsole.MarkupLine("[cyan]km delete draft-proposal --nodes work[/]");
        AnsiConsole.MarkupLine("[dim]Remove only from your work collection[/]");
        AnsiConsole.WriteLine();
    }

    private void ShowNodesExamples()
    {
        AnsiConsole.Write(new Rule("[blue]🗂  COLLECTIONS - Your note spaces[/]").LeftJustified());
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]See your collections[/]");
        AnsiConsole.MarkupLine("[cyan]km nodes[/]");
        AnsiConsole.MarkupLine("[dim]View all your note collections (personal, work, etc.)[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Export as JSON[/]");
        AnsiConsole.MarkupLine("[cyan]km nodes --format json[/]");
        AnsiConsole.MarkupLine("[dim]Get collection info in structured format[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Export as YAML[/]");
        AnsiConsole.MarkupLine("[cyan]km nodes --format yaml[/]");
        AnsiConsole.MarkupLine("[dim]Easy-to-read collection settings[/]");
        AnsiConsole.WriteLine();
    }

    private void ShowConfigExamples()
    {
        AnsiConsole.Write(new Rule("[yellow]📝 SETTINGS - Manage your setup[/]").LeftJustified());
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Check your settings[/]");
        AnsiConsole.MarkupLine("[cyan]km config[/]");
        AnsiConsole.MarkupLine("[dim]See where your settings file is and what's configured[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]View collections setup[/]");
        AnsiConsole.MarkupLine("[cyan]km config --show-nodes[/]");
        AnsiConsole.MarkupLine("[dim]See how your note collections are organized[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]View cache settings[/]");
        AnsiConsole.MarkupLine("[cyan]km config --show-cache[/]");
        AnsiConsole.MarkupLine("[dim]Check your caching configuration[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Create new settings[/]");
        AnsiConsole.MarkupLine("[cyan]km config --create[/]");
        AnsiConsole.MarkupLine("[dim]Guided setup to create a new configuration[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Use different settings[/]");
        AnsiConsole.MarkupLine("[cyan]km --config my-settings.json search \"medical records\"[/]");
        AnsiConsole.MarkupLine("[dim]Use a specific settings file for this command[/]");
        AnsiConsole.WriteLine();
    }

    private void ShowAdvancedExamples()
    {
        AnsiConsole.Write(new Rule("[bold purple]🚀 POWER USER TIPS[/]").LeftJustified());
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold underline]Combine keyword and semantic search[/]");
        AnsiConsole.MarkupLine("[cyan]km search 'medical records' --indexes text-search,meaning-search[/]");
        AnsiConsole.MarkupLine("[dim]Find by exact words AND by meaning for better results[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold underline]Prioritize certain collections[/]");
        AnsiConsole.MarkupLine("[cyan]km search 'deadlines' --node-weights work:1.5,personal:0.8,archive:0.3[/]");
        AnsiConsole.MarkupLine("[dim]Make work notes show up first, personal second, archives last[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold underline]Skip slow searches[/]");
        AnsiConsole.MarkupLine("[cyan]km search 'insurance' --exclude-indexes experimental-search[/]");
        AnsiConsole.MarkupLine("[dim]Skip searches that are still being tested[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold underline]See more context[/]");
        AnsiConsole.MarkupLine("[cyan]km search 'prescription' --snippet --snippet-length 500[/]");
        AnsiConsole.MarkupLine("[dim]Show more text around your matches[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold underline]Wait for recent additions[/]");
        AnsiConsole.MarkupLine("[cyan]km search 'today appointment' --wait-for-indexing[/]");
        AnsiConsole.MarkupLine("[dim]Make sure freshly added notes are included[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold underline]Test your search[/]");
        AnsiConsole.MarkupLine("[cyan]km search --validate 'title:study AND (tags:exam OR tags:final)'[/]");
        AnsiConsole.MarkupLine("[dim]Check if your search query is valid before running it[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold underline]Complex filtering[/]");
        AnsiConsole.MarkupLine("[cyan]km search '(title:invoice OR content:payment) AND tags:important'[/]");
        AnsiConsole.MarkupLine("[dim]Find important financial documents with flexible matching[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold underline]Batch import with tags[/]");
        AnsiConsole.MarkupLine("[cyan]km --config work-setup.json put contracts/*.pdf --tags legal,2024[/]");
        AnsiConsole.MarkupLine("[dim]Import all contracts using work settings[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold underline]Save from clipboard or file[/]");
        AnsiConsole.MarkupLine("[cyan]cat lecture-notes.txt | km put --id lecture-dec01 --tags education,cs101[/]");
        AnsiConsole.MarkupLine("[dim]Save piped content with your own ID and tags[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold underline]Export and process results[/]");
        AnsiConsole.MarkupLine("[cyan]km search 'project status' --format json | jq '.results'[/]");
        AnsiConsole.MarkupLine("[dim]Get results in JSON to process with other tools[/]");
        AnsiConsole.WriteLine();

        // Helpful tips section
        AnsiConsole.Write(new Rule("[grey]💡 Helpful Tips[/]").LeftJustified());
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]• Use [cyan]--format json[/] to export results for other programs[/]");
        AnsiConsole.MarkupLine("[dim]• Combine [cyan]--highlight[/] and [cyan]--snippet[/] to see where your words appear[/]");
        AnsiConsole.MarkupLine("[dim]• Set [cyan]--min-relevance[/] higher (0.6-0.8) for more precise matches[/]");
        AnsiConsole.MarkupLine("[dim]• Use [cyan]--nodes[/] to search specific collections when you know where it is[/]");
        AnsiConsole.MarkupLine("[dim]• Type [cyan]km <command> --help[/] to see all options for that command[/]");
        AnsiConsole.MarkupLine("[dim]• Read [cyan]CONFIGURATION.md[/] for complete setup guide[/]");
    }
}
