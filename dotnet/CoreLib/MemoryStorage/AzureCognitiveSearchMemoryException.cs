// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.SemanticMemory.Client;

namespace Microsoft.SemanticMemory.Core.MemoryStorage;

public class AzureCognitiveSearchMemoryException : SemanticMemoryException
{
    /// <inheritdoc />
    public AzureCognitiveSearchMemoryException()
    {
    }

    /// <inheritdoc />
    public AzureCognitiveSearchMemoryException(string? message) : base(message)
    {
    }

    /// <inheritdoc />
    public AzureCognitiveSearchMemoryException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
