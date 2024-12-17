// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.MemoryDb.AzureAISearch;

public class AzureAISearchMemoryException : KernelMemoryException
{
    /// <inheritdoc />
    public AzureAISearchMemoryException(bool? isTransient = null)
    {
        this.IsTransient = isTransient;
    }

    /// <inheritdoc />
    public AzureAISearchMemoryException(string message, bool? isTransient = null) : base(message)
    {
        this.IsTransient = isTransient;
    }

    /// <inheritdoc />
    public AzureAISearchMemoryException(string message, Exception? innerException, bool? isTransient = null) : base(message, innerException)
    {
        this.IsTransient = isTransient;
    }
}

