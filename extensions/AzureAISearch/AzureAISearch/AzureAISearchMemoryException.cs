// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.MemoryDb.AzureAISearch;

public class AzureAISearchMemoryException : KernelMemoryException
{
    /// <inheritdoc />
    public AzureAISearchMemoryException()
    {
    }

    /// <inheritdoc />
    public AzureAISearchMemoryException(string? message) : base(message)
    {
    }

    /// <inheritdoc />
    public AzureAISearchMemoryException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
