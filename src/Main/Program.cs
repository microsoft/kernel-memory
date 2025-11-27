// Copyright (c) Microsoft. All rights reserved.
﻿using KernelMemory.Main.CLI.Commands;
using Spectre.Console.Cli;

namespace KernelMemory.Main;

/// <summary>
/// Multi-mode entry point for Kernel Memory.
/// Supports CLI, MCP Server, Web Service, and RPC modes.
/// </summary>
internal sealed class Program
{
    // Static readonly arrays for command examples (CA1861)
    private static readonly string[] s_upsertExample1 = ["upsert", "\"Hello, world!\""];
    private static readonly string[] s_upsertExample2 = ["upsert", "\"Some content\"", "--id", "my-id-123"];
    private static readonly string[] s_upsertExample3 = ["upsert", "\"Tagged content\"", "--tags", "important,todo"];
    private static readonly string[] s_getExample1 = ["get", "abc123"];
    private static readonly string[] s_getExample2 = ["get", "abc123", "--full"];
    private static readonly string[] s_getExample3 = ["get", "abc123", "-f", "json"];
    private static readonly string[] s_deleteExample1 = ["delete", "abc123"];
    private static readonly string[] s_deleteExample2 = ["delete", "abc123", "-v", "quiet"];
    private static readonly string[] s_listExample1 = ["list"];
    private static readonly string[] s_listExample2 = ["list", "--skip", "20", "--take", "10"];
    private static readonly string[] s_listExample3 = ["list", "-f", "json"];
    private static readonly string[] s_nodesExample1 = ["nodes"];
    private static readonly string[] s_nodesExample2 = ["nodes", "-f", "yaml"];
    private static readonly string[] s_configExample1 = ["config"];
    private static readonly string[] s_configExample2 = ["config", "--show-nodes"];
    private static readonly string[] s_configExample3 = ["config", "--show-cache"];

    /// <summary>
    /// Main entry point - routes to appropriate mode based on first argument.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code (0 = success, 1 = user error, 2 = system error).</returns>
    private static async Task<int> Main(string[] args)
    {
        // Simple mode detection - check first argument
        var mode = args.Length > 0 ? args[0].ToLowerInvariant() : "cli";

        return mode switch
        {
            "mcpserver" or "mcp" => RunMcpModeAsync(),
            "webservice" or "web" => RunWebModeAsync(),
            "rpc" => RunRpcModeAsync(),
            _ => await RunCliModeAsync(args).ConfigureAwait(false)
        };
    }

    /// <summary>
    /// Runs in CLI mode using Spectre.Console.Cli.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code.</returns>
    private static async Task<int> RunCliModeAsync(string[] args)
    {
        var app = new CommandApp();

        app.Configure(config =>
        {
            config.SetApplicationName("km");

            // Add all commands
            config.AddCommand<UpsertCommand>("upsert")
                .WithDescription("Upload or update content")
                .WithExample(s_upsertExample1)
                .WithExample(s_upsertExample2)
                .WithExample(s_upsertExample3);

            config.AddCommand<GetCommand>("get")
                .WithDescription("Fetch content by ID")
                .WithExample(s_getExample1)
                .WithExample(s_getExample2)
                .WithExample(s_getExample3);

            config.AddCommand<DeleteCommand>("delete")
                .WithDescription("Delete content by ID")
                .WithExample(s_deleteExample1)
                .WithExample(s_deleteExample2);

            config.AddCommand<ListCommand>("list")
                .WithDescription("List all content with pagination")
                .WithExample(s_listExample1)
                .WithExample(s_listExample2)
                .WithExample(s_listExample3);

            config.AddCommand<NodesCommand>("nodes")
                .WithDescription("List all configured nodes")
                .WithExample(s_nodesExample1)
                .WithExample(s_nodesExample2);

            config.AddCommand<ConfigCommand>("config")
                .WithDescription("Query configuration")
                .WithExample(s_configExample1)
                .WithExample(s_configExample2)
                .WithExample(s_configExample3);

            // Validate examples at startup (optional)
            config.ValidateExamples();
        });

        return await app.RunAsync(args).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs in MCP (Model Context Protocol) server mode.
    /// </summary>
    /// <returns>Exit code.</returns>
    private static int RunMcpModeAsync()
    {
        Console.Error.WriteLine("Error: MCP mode not yet implemented");
        Console.Error.WriteLine("This feature will allow ChatGPT to connect to Kernel Memory nodes");
        return Constants.ExitCodeSystemError;
    }

    /// <summary>
    /// Runs in web service mode.
    /// </summary>
    /// <returns>Exit code.</returns>
    private static int RunWebModeAsync()
    {
        Console.Error.WriteLine("Error: Web service mode not yet implemented");
        Console.Error.WriteLine("This feature will publish memory nodes as a web API");
        return Constants.ExitCodeSystemError;
    }

    /// <summary>
    /// Runs in RPC mode for Electron app integration.
    /// </summary>
    /// <returns>Exit code.</returns>
    private static int RunRpcModeAsync()
    {
        Console.Error.WriteLine("Error: RPC mode not yet implemented");
        Console.Error.WriteLine("This feature will enable Electron app communication");
        return Constants.ExitCodeSystemError;
    }
}