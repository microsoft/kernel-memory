// Copyright (c) Microsoft. All rights reserved.

namespace KernelMemory.Main.CLI;

/// <summary>
/// Routes execution to appropriate mode (CLI, MCP, Web, RPC) based on arguments.
/// Extracted from Program.cs for testability.
/// </summary>
public sealed class ModeRouter
{
    /// <summary>
    /// Detects the execution mode from command line arguments.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Detected mode: "cli", "mcp", "web", or "rpc".</returns>
    public string DetectMode(string[] args)
    {
        if (args.Length == 0)
        {
            return "cli";
        }

        var firstArg = args[0].ToLowerInvariant();
        return firstArg switch
        {
            "mcpserver" or "mcp" => "mcp",
            "webservice" or "web" => "web",
            "rpc" => "rpc",
            _ => "cli"
        };
    }

    /// <summary>
    /// Handles unimplemented mode by writing error to stderr.
    /// </summary>
    /// <param name="mode">Mode name to display in error message.</param>
    /// <param name="description">Description of the unimplemented feature.</param>
    /// <returns>System error exit code.</returns>
    public int HandleUnimplementedMode(string mode, string description)
    {
        Console.Error.WriteLine($"Error: {mode} mode not yet implemented");
        Console.Error.WriteLine(description);
        return Constants.ExitCodeSystemError;
    }
}
