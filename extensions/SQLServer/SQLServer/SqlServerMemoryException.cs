// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.MemoryDb.SQLServer;

/// <summary>
/// Represents a SQL Server memory store exception.
/// </summary>
public class SqlServerMemoryException : KernelMemoryException
{
    /// <inheritdoc />
    public SqlServerMemoryException()
    {
    }

    /// <inheritdoc />
    public SqlServerMemoryException(string? message) : base(message)
    {
    }

    /// <inheritdoc />
    public SqlServerMemoryException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
