// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.SemanticMemory.Core20;

namespace Microsoft.SemanticMemory.Core.Configuration;

public class ConfigurationException : SemanticMemoryException
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
