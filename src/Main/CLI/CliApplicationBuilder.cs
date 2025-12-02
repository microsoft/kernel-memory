// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Core.Config;
using KernelMemory.Main.CLI.Commands;
using KernelMemory.Main.CLI.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace KernelMemory.Main.CLI;

/// <summary>
/// Builds and configures the CLI application with all commands.
/// Extracted from Program.cs for testability.
/// </summary>
public sealed class CliApplicationBuilder
{
    // Static readonly arrays for command examples (CA1861 compliance)
    private static readonly string[] s_upsertExample1 = new[] { "put", "\"Hello, world!\"" };
    private static readonly string[] s_upsertExample2 = new[] { "put", "\"Some content\"", "--id", "my-id-123" };
    private static readonly string[] s_upsertExample3 = new[] { "put", "\"Tagged content\"", "--tags", "important,todo" };
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
    private static readonly string[] s_configExample4 = new[] { "config", "--create" };
    private static readonly string[] s_searchExample1 = new[] { "search", "kubernetes" };
    private static readonly string[] s_searchExample2 = new[] { "search", "content:kubernetes AND tags:production" };
    private static readonly string[] s_searchExample3 = new[] { "search", "kubernetes", "--limit", "10" };
    private static readonly string[] s_searchExample4 = new[] { "search", "{\"content\": \"kubernetes\"}", "--format", "json" };
    private static readonly string[] s_examplesExample1 = new[] { "examples" };
    private static readonly string[] s_examplesExample2 = new[] { "examples", "--command", "search" };

    /// <summary>
    /// Creates and configures a CommandApp with all CLI commands.
    /// Loads configuration early and injects it via DI.
    /// </summary>
    /// <param name="args">Command line arguments (used to extract --config flag).</param>
    /// <returns>A configured CommandApp ready to execute commands.</returns>
    public CommandApp Build(string[]? args = null)
    {
        // 1. Determine config path from args early (before command execution)
        string configPath = this.DetermineConfigPath(args ?? []);

        // 2. Load config ONCE (happens before any command runs)
        AppConfig config = ConfigParser.LoadFromFile(configPath);

        // 3. Create DI container and register AppConfig as singleton
        ServiceCollection services = new();
        services.AddSingleton(config);

        // Also register the config path so commands can access it
        services.AddSingleton(new ConfigPathService(configPath));

        // 4. Create type registrar for Spectre.Console.Cli DI integration
        TypeRegistrar registrar = new(services);

        // 5. Build CommandApp with DI support
        CommandApp app = new(registrar);
        this.Configure(app);
        return app;
    }

    /// <summary>
    /// Determines the configuration file path from command line arguments.
    /// Scans args for --config or -c flag. Falls back to default ~/.km/config.json.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Path to configuration file.</returns>
    private string DetermineConfigPath(string[] args)
    {
        // Simple string scanning for --config or -c flag
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--config" || args[i] == "-c")
            {
                return args[i + 1];
            }
        }

        // Default: ~/.km/config.json
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Constants.DefaultConfigDirName,
            Constants.DefaultConfigFileName);
    }

    /// <summary>
    /// Configures the CommandApp with all commands and examples.
    /// Made public to allow tests to reuse command configuration.
    /// </summary>
    /// <param name="app">The CommandApp to configure.</param>
    public void Configure(CommandApp app)
    {
        app.Configure(config =>
        {
            config.SetApplicationName("km");

            // Put command (HTTP-style naming for upsert operation)
            config.AddCommand<UpsertCommand>("put")
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
                .WithExample(s_configExample3)
                .WithExample(s_configExample4);

            // Search command
            config.AddCommand<SearchCommand>("search")
                .WithDescription("Search content across nodes and indexes")
                .WithExample(s_searchExample1)
                .WithExample(s_searchExample2)
                .WithExample(s_searchExample3)
                .WithExample(s_searchExample4);

            // Examples command
            config.AddCommand<ExamplesCommand>("examples")
                .WithDescription("Show usage examples for all commands")
                .WithExample(s_examplesExample1)
                .WithExample(s_examplesExample2);

            config.ValidateExamples();
        });
    }
}