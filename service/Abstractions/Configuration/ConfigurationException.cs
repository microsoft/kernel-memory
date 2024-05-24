// Copyright (c) Microsoft. All rights reserved.

using System;

// ReSharper disable CheckNamespace
namespace Microsoft.KernelMemory;

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
