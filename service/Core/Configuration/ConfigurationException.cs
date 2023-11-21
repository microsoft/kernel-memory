// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.Configuration;

public class ConfigurationException : KernelMemoryException
{
    /// <inheritdoc />
    public ConfigurationException()
    {
    }

    /// <inheritdoc />
    public ConfigurationException(string? message) : base(message)
    {
    }

    /// <inheritdoc />
    public ConfigurationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
