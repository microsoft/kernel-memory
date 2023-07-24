// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.SemanticKernel.SemanticMemory.Core.Configuration;

public class ConfigurationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with a default message.
    /// </summary>
    public ConfigurationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with its message set to <paramref name="message"/>.
    /// </summary>
    /// <param name="message">A string that describes the error.</param>
    public ConfigurationException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with its message set to <paramref name="message"/>.
    /// </summary>
    /// <param name="message">A string that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConfigurationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
