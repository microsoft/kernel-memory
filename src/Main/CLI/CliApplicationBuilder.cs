// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Main.CLI.Commands;
using Spectre.Console.Cli;

namespace KernelMemory.Main.CLI;

/// <summary>
/// Builds and configures the CLI application with all commands.
/// Extracted from Program.cs for testability.
/// </summary>
public sealed class CliApplicationBuilder
{
    /// <summary>
    /// Creates and configures a CommandApp with all CLI commands.
    /// </summary>
    public CommandApp Build()
    {
        var app = new CommandApp();
        this.Configure(app);
        return app;
    }

    /// <summary>
    /// Configures the CommandApp with all commands and examples.
    /// </summary>
    internal void Configure(CommandApp app)
    {
        app.Configure(config =>
        {
            config.SetApplicationName("km");

            // Upsert command
            config.AddCommand<UpsertCommand>("upsert")
                .WithDescription("Upload or update content")
                .WithExample(new[] { "upsert", "\"Hello, world!\"" })
                .WithExample(new[] { "upsert", "\"Some content\"", "--id", "my-id-123" })
                .WithExample(new[] { "upsert", "\"Tagged content\"", "--tags", "important,todo" });

            // Get command
            config.AddCommand<GetCommand>("get")
                .WithDescription("Fetch content by ID")
                .WithExample(new[] { "get", "abc123" })
                .WithExample(new[] { "get", "abc123", "--full" })
                .WithExample(new[] { "get", "abc123", "-f", "json" });

            // Delete command
            config.AddCommand<DeleteCommand>("delete")
                .WithDescription("Delete content by ID")
                .WithExample(new[] { "delete", "abc123" })
                .WithExample(new[] { "delete", "abc123", "-v", "quiet" });

            // List command
            config.AddCommand<ListCommand>("list")
                .WithDescription("List all content with pagination")
                .WithExample(new[] { "list" })
                .WithExample(new[] { "list", "--skip", "20", "--take", "10" })
                .WithExample(new[] { "list", "-f", "json" });

            // Nodes command
            config.AddCommand<NodesCommand>("nodes")
                .WithDescription("List all configured nodes")
                .WithExample(new[] { "nodes" })
                .WithExample(new[] { "nodes", "-f", "yaml" });

            // Config command
            config.AddCommand<ConfigCommand>("config")
                .WithDescription("Query configuration")
                .WithExample(new[] { "config" })
                .WithExample(new[] { "config", "--show-nodes" })
                .WithExample(new[] { "config", "--show-cache" });

            config.ValidateExamples();
        });
    }
}
