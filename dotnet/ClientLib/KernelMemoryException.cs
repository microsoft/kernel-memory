// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory;

/// <summary>
/// Provides the base exception from which all Kernel Memory exceptions derive.
/// </summary>
public class KernelMemoryException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KernelMemoryException"/> class with a default message.
    /// </summary>
    public KernelMemoryException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KernelMemoryException"/> class with its message set to <paramref name="message"/>.
    /// </summary>
    /// <param name="message">A string that describes the error.</param>
    public KernelMemoryException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KernelMemoryException"/> class with its message set to <paramref name="message"/>.
    /// </summary>
    /// <param name="message">A string that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public KernelMemoryException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
