// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Main.CLI;

namespace KernelMemory.Main;

/// <summary>
/// Multi-mode entry point for Kernel Memory.
/// Thin entry point - delegates to CliApplicationBuilder and ModeRouter.
/// </summary>
internal sealed class Program
{
    /// <summary>
    /// Main entry point - routes to appropriate mode based on arguments.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code (0 = success, 1 = user error, 2 = system error).</returns>
    private static async Task<int> Main(string[] args)
    {
        var router = new ModeRouter();
        var mode = router.DetectMode(args);

        return mode switch
        {
            "mcp" => router.HandleUnimplementedMode("MCP", "This feature will allow MCP clients to connect to Kernel Memory nodes"),
            "web" => router.HandleUnimplementedMode("Web service", "This feature will publish memory nodes as a web API"),
            "rpc" => router.HandleUnimplementedMode("RPC", "This feature will enable external apps communication"),
            _ => await RunCliModeAsync(args).ConfigureAwait(false)
        };
    }

    /// <summary>
    /// Runs in CLI mode using Spectre.Console.Cli.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code from CLI execution.</returns>
    private static async Task<int> RunCliModeAsync(string[] args)
    {
        var builder = new CliApplicationBuilder();
        var app = builder.Build();
        return await app.RunAsync(args).ConfigureAwait(false);
    }
}
