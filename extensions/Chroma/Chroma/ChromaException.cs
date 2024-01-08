// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.MemoryDb.Chroma;

/// <summary>
/// Base exception for all the exceptions thrown by the Postgres connector for KernelMemory
/// </summary>
public class ChromaException : KernelMemoryException
{
    /// <inheritdoc />
    public ChromaException() { }

    /// <inheritdoc />
    public ChromaException(string message) : base(message) { }

    /// <inheritdoc />
    public ChromaException(string message, Exception? innerException) : base(message, innerException) { }
}
