// Copyright (c) Microsoft. All rights reserved.
using System.ComponentModel;
using Serilog.Events;
using Spectre.Console;
using Spectre.Console.Cli;

namespace KernelMemory.Main.CLI;

/// <summary>
/// Global options shared across all CLI commands.
/// </summary>
public class GlobalOptions : CommandSettings
{
    [CommandOption("-c|--config")]
    [Description("Path to config file (default: ~/.km/config.json)")]
    public string? ConfigPath { get; init; }

    [CommandOption("-n|--node")]
    [Description("Node name to use (default: first in config)")]
    public string? NodeName { get; init; }

    [CommandOption("-f|--format")]
    [Description("Output format: human, json, yaml")]
    [DefaultValue("human")]
    public string Format { get; init; } = "human";

    [CommandOption("-v|--verbosity")]
    [Description("Log verbosity: silent, quiet, normal, verbose")]
    [DefaultValue("normal")]
    public string Verbosity { get; init; } = "normal";

    [CommandOption("--log-file")]
    [Description("Path to log file (enables file logging)")]
    public string? LogFile { get; init; }

    [CommandOption("--no-color")]
    [Description("Disable colored output")]
    public bool NoColor { get; init; }

    /// <summary>
    /// Validates the global options.
    /// </summary>
    public override ValidationResult Validate()
    {
        var validFormats = new[] { "human", "json", "yaml" };
        if (!validFormats.Contains(this.Format.ToLowerInvariant()))
        {
            return ValidationResult.Error("Format must be: human, json, or yaml");
        }

        var validVerbosities = new[] { "silent", "quiet", "normal", "verbose" };
        if (!validVerbosities.Contains(this.Verbosity.ToLowerInvariant()))
        {
            return ValidationResult.Error("Verbosity must be: silent, quiet, normal, or verbose");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Maps the verbosity string to a Serilog LogEventLevel.
    /// silent = Fatal only, quiet = Warning+, normal = Information+, verbose = Debug+
    /// </summary>
    /// <returns>The corresponding Serilog LogEventLevel.</returns>
    public LogEventLevel GetLogLevel()
    {
        return this.Verbosity.ToLowerInvariant() switch
        {
            "silent" => LogEventLevel.Fatal,
            "quiet" => LogEventLevel.Warning,
            "verbose" => LogEventLevel.Debug,
            _ => LogEventLevel.Information // "normal" and default
        };
    }
}
