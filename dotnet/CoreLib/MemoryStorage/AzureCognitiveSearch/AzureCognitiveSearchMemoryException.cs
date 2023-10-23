// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.MemoryStorage.AzureCognitiveSearch;

public class AzureCognitiveSearchMemoryException : KernelMemoryException
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
