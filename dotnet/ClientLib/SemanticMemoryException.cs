// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.SemanticMemory.Client;

/// <summary>
/// Provides the base exception from which all Semantic Kernel exceptions derive.
/// </summary>
public class SemanticMemoryException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticMemoryException"/> class with a default message.
    /// </summary>
    public SemanticMemoryException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticMemoryException"/> class with its message set to <paramref name="message"/>.
    /// </summary>
    /// <param name="message">A string that describes the error.</param>
    public SemanticMemoryException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticMemoryException"/> class with its message set to <paramref name="message"/>.
    /// </summary>
    /// <param name="message">A string that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public SemanticMemoryException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
