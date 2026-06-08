// Copyright (c) Microsoft. All rights reserved.

namespace KernelMemory.Main.CLI.Exceptions;

/// <summary>
/// Exception thrown when database doesn't exist yet.
/// This is an expected state on first run - not an error.
/// </summary>
public sealed class DatabaseNotFoundException : Exception
{
    /// <summary>
    /// Gets the path where the database was expected to be found.
    /// </summary>
    public string DatabasePath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseNotFoundException"/> class.
    /// </summary>
    /// <param name="dbPath">The path where the database was expected.</param>
    public DatabaseNotFoundException(string dbPath)
        : base($"No content database found at '{dbPath}'. This is your first run.")
    {
        this.DatabasePath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseNotFoundException"/> class.
    /// </summary>
    public DatabaseNotFoundException()
        : base("No content database found.")
    {
        this.DatabasePath = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public DatabaseNotFoundException(string? message, Exception? innerException)
        : base(message, innerException)
    {
        this.DatabasePath = string.Empty;
    }
}
