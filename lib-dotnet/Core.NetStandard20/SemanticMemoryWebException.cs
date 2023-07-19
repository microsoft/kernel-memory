// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.SemanticKernel.SemanticMemory.Core20;

public class SemanticMemoryWebException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticMemoryWebException"/> class with a default message.
    /// </summary>
    public SemanticMemoryWebException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticMemoryWebException"/> class with its message set to <paramref name="message"/>.
    /// </summary>
    /// <param name="message">A string that describes the error.</param>
    public SemanticMemoryWebException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticMemoryWebException"/> class with its message set to <paramref name="message"/>.
    /// </summary>
    /// <param name="message">A string that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public SemanticMemoryWebException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
