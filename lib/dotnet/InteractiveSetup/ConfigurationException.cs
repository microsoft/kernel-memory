// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.SemanticKernel.SemanticMemory.InteractiveSetup;

public class SetupException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetupException"/> class with a default message.
    /// </summary>
    public SetupException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SetupException"/> class with its message set to <paramref name="message"/>.
    /// </summary>
    /// <param name="message">A string that describes the error.</param>
    public SetupException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SetupException"/> class with its message set to <paramref name="message"/>.
    /// </summary>
    /// <param name="message">A string that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public SetupException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
