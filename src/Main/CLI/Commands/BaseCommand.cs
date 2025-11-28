// Copyright (c) Microsoft. All rights reserved.
using System.Diagnostics.CodeAnalysis;
using KernelMemory.Core.Config;
using KernelMemory.Core.Config.ContentIndex;
using KernelMemory.Core.Storage;
using KernelMemory.Main.CLI.Exceptions;
using KernelMemory.Main.CLI.OutputFormatters;
using KernelMemory.Main.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

namespace KernelMemory.Main.CLI.Commands;

/// <summary>
/// Base class for all CLI commands providing shared initialization logic.
/// Config is injected via constructor (loaded once in CliApplicationBuilder).
/// </summary>
/// <typeparam name="TSettings">The command settings type, must inherit from GlobalOptions.</typeparam>
public abstract class BaseCommand<TSettings> : AsyncCommand<TSettings>
    where TSettings : GlobalOptions
{
    private readonly AppConfig _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseCommand{TSettings}"/> class.
    /// </summary>
    /// <param name="config">Application configuration (injected by DI).</param>
    protected BaseCommand(AppConfig config)
    {
        this._config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Gets the injected application configuration.
    /// </summary>
    protected AppConfig Config => this._config;

    /// <summary>
    /// Initializes command dependencies: node and formatter.
    /// Config is already injected via constructor (no file I/O).
    /// </summary>
    /// <param name="settings">The command settings.</param>
    /// <returns>Tuple of (config, node, formatter).</returns>
    protected (AppConfig config, NodeConfig node, IOutputFormatter formatter)
        Initialize(TSettings settings)
    {
        // Config already loaded and injected via constructor
        var config = this._config;

        // Select node
        if (config.Nodes.Count == 0)
        {
            throw new InvalidOperationException("No nodes configured. Please create a configuration file.");
        }

        var nodeName = settings.NodeName ?? config.Nodes.Keys.First();
        if (!config.Nodes.TryGetValue(nodeName, out var node))
        {
            throw new InvalidOperationException($"Node '{nodeName}' not found in configuration.");
        }

        // Create formatter
        var formatter = OutputFormatterFactory.Create(settings);

        return (config, node, formatter);
    }

    /// <summary>
    /// Creates a ContentService instance for the specified node.
    /// </summary>
    /// <param name="node">The node configuration.</param>
    /// <param name="readonlyMode">If true, will not create directories or database. Will fail if database doesn't exist.</param>
    /// <returns>A ContentService instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when database doesn't exist in readonly mode.</exception>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "DbContext ownership is transferred to ContentStorageService which handles disposal")]
    protected ContentService CreateContentService(NodeConfig node, bool readonlyMode = false)
    {
        // Get SQLite database path from node config
        if (node.ContentIndex is not SqliteContentIndexConfig sqliteConfig)
        {
            throw new InvalidOperationException($"Node '{node.Id}' does not use SQLite content index.");
        }

        var dbPath = sqliteConfig.Path;

        // In readonly mode, verify database exists without creating it
        if (readonlyMode)
        {
            if (!File.Exists(dbPath))
            {
                throw new DatabaseNotFoundException(dbPath);
            }
        }
        else
        {
            // Write mode: Ensure directory exists
            var dbDir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }
        }

        // Create connection string
        var connectionString = "Data Source=" + dbPath;

        // Create DbContext
        var optionsBuilder = new DbContextOptionsBuilder<ContentStorageDbContext>();
        optionsBuilder.UseSqlite(connectionString);
        var context = new ContentStorageDbContext(optionsBuilder.Options);

        // In write mode, ensure database is created
        // In readonly mode, the database must already exist (checked above)
        if (!readonlyMode)
        {
            context.Database.EnsureCreated();
        }

        // Create dependencies
        var cuidGenerator = new CuidGenerator();
        var logger = this.CreateLogger();

        // Create storage service
        var storage = new ContentStorageService(context, cuidGenerator, logger);

        // Create and return content service
        return new ContentService(storage, node.Id);
    }

    /// <summary>
    /// Creates a simple console logger for ContentStorageService.
    /// </summary>
    /// <returns>A logger instance.</returns>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "LoggerFactory lifetime is managed by the logger infrastructure. CLI commands are short-lived and disposing would terminate logging prematurely.")]
    private ILogger<ContentStorageService> CreateLogger()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        return loggerFactory.CreateLogger<ContentStorageService>();
    }

    /// <summary>
    /// Handles exceptions and returns appropriate exit code.
    /// </summary>
    /// <param name="ex">The exception to handle.</param>
    /// <param name="formatter">The output formatter for error messages.</param>
    /// <returns>Exit code (1 for user errors, 2 for system errors).</returns>
    protected int HandleError(Exception ex, IOutputFormatter formatter)
    {
        formatter.FormatError(ex.Message);

        // User errors: InvalidOperationException, ArgumentException
        if (ex is InvalidOperationException or ArgumentException)
        {
            return Constants.ExitCodeUserError;
        }

        // System errors: everything else
        return Constants.ExitCodeSystemError;
    }
}
