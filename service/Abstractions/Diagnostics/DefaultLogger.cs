// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.KernelMemory.Diagnostics;

/// <summary>
/// Create and cache a logger instance using the same
/// configuration sources supported by Kernel Memory config.
/// </summary>
/// <typeparam name="T">Type of the class using the logger. The type name
/// is used to decorate log entries, providing information about the log source.</typeparam>
public static class DefaultLogger<T>
{
    public static readonly ILogger<T> Instance = DefaultLogger.Factory.CreateLogger<T>();
}

/// <summary>
/// Either create a local default log factory, or allow an external
/// application to set the log factory to be used to instantiate loggers.
/// </summary>
public static class DefaultLogger
{
    private const string LoggingSection = "Logging";
    private const string LogLevelSection = "LogLevel";
    private const string DefaultCategoryKey = "Default";
    private const string AspNetEnvironment = "ASPNETCORE_ENVIRONMENT";

    private static ILoggerFactory? s_factory = null;

    public static ILoggerFactory Factory
    {
        get { return s_factory ??= DefaultLogFactory(); }
        set { s_factory = value; }
    }

    private static ILoggerFactory DefaultLogFactory()
    {
        var env = Environment.GetEnvironmentVariable(AspNetEnvironment) ?? string.Empty;
        var cfgBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);
        cfgBuilder.AddEnvironmentVariables();
        IConfigurationRoot cfg = cfgBuilder.Build();

        return LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            foreach (IConfigurationSection section in cfg.GetSection(LoggingSection).GetSection(LogLevelSection).GetChildren())
            {
                if (!TryGetLogLevel(section.Value, out LogLevel level)) { continue; }

                builder.AddFilter(category: (section.Key == DefaultCategoryKey) ? null : section.Key, level: level);
            }
        });
    }

    private static bool TryGetLogLevel(string? value, out LogLevel level)
    {
        if (string.IsNullOrEmpty(value))
        {
            level = LogLevel.None;
            return false;
        }

        if (Enum.TryParse(value, true, out level))
        {
            return true;
        }

        throw new ConfigurationException($"Logger: log level '{value}' not supported");
    }
}
