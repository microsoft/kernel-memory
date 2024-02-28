// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.MemoryDb.AzureCosmosDBMongoVCore;

public class AzureCosmosDBMongoVCorehMemoryException : KernelMemoryException
{
    /// <inheritdoc />
    public AzureCosmosDBMongoVCorehMemoryException()
    {
    }

    /// <inheritdoc />
    public AzureCosmosDBMongoVCoreMemoryException(string? message) : base(message)
    {
    }

    /// <inheritdoc />
    public AzureCosmosDBMongoVCoreMemoryException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
