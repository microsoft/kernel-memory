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
    // Static readonly arrays for command examples (CA1861 compliance)
    private static readonly string[] s_upsertExample1 = new[] { "upsert", "\"Hello, world!\"" };
    private static readonly string[] s_upsertExample2 = new[] { "upsert", "\"Some content\"", "--id", "my-id-123" };
    private static readonly string[] s_upsertExample3 = new[] { "upsert", "\"Tagged content\"", "--tags", "important,todo" };
    private static readonly string[] s_getExample1 = new[] { "get", "abc123" };
    private static readonly string[] s_getExample2 = new[] { "get", "abc123", "--full" };
    private static readonly string[] s_getExample3 = new[] { "get", "abc123", "-f", "json" };
    private static readonly string[] s_deleteExample1 = new[] { "delete", "abc123" };
    private static readonly string[] s_deleteExample2 = new[] { "delete", "abc123", "-v", "quiet" };
    private static readonly string[] s_listExample1 = new[] { "list" };
    private static readonly string[] s_listExample2 = new[] { "list", "--skip", "20", "--take", "10" };
    private static readonly string[] s_listExample3 = new[] { "list", "-f", "json" };
    private static readonly string[] s_nodesExample1 = new[] { "nodes" };
    private static readonly string[] s_nodesExample2 = new[] { "nodes", "-f", "yaml" };
    private static readonly string[] s_configExample1 = new[] { "config" };
    private static readonly string[] s_configExample2 = new[] { "config", "--show-nodes" };
    private static readonly string[] s_configExample3 = new[] { "config", "--show-cache" };

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
    /// <param name="app">The CommandApp to configure.</param>
    internal void Configure(CommandApp app)
    {
        app.Configure(config =>
        {
            config.SetApplicationName("km");

            // Upsert command
            config.AddCommand<UpsertCommand>("upsert")
                .WithDescription("Upload or update content")
                .WithExample(s_upsertExample1)
                .WithExample(s_upsertExample2)
                .WithExample(s_upsertExample3);

            // Get command
            config.AddCommand<GetCommand>("get")
                .WithDescription("Fetch content by ID")
                .WithExample(s_getExample1)
                .WithExample(s_getExample2)
                .WithExample(s_getExample3);

            // Delete command
            config.AddCommand<DeleteCommand>("delete")
                .WithDescription("Delete content by ID")
                .WithExample(s_deleteExample1)
                .WithExample(s_deleteExample2);

            // List command
            config.AddCommand<ListCommand>("list")
                .WithDescription("List all content with pagination")
                .WithExample(s_listExample1)
                .WithExample(s_listExample2)
                .WithExample(s_listExample3);

            // Nodes command
            config.AddCommand<NodesCommand>("nodes")
                .WithDescription("List all configured nodes")
                .WithExample(s_nodesExample1)
                .WithExample(s_nodesExample2);

            // Config command
            config.AddCommand<ConfigCommand>("config")
                .WithDescription("Query configuration")
                .WithExample(s_configExample1)
                .WithExample(s_configExample2)
                .WithExample(s_configExample3);

            config.ValidateExamples();
        });
    }
}
