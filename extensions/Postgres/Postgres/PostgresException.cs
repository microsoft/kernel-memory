// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.Postgres;

/// <summary>
/// Base exception for all the exceptions thrown by the Postgres connector for KernelMemory
/// </summary>
public class PostgresException : KernelMemoryException
{
    /// <inheritdoc />
    public PostgresException() { }

    /// <inheritdoc />
    public PostgresException(string message) : base(message) { }

    /// <inheritdoc />
    public PostgresException(string message, Exception? innerException) : base(message, innerException) { }
}
