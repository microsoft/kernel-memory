// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.MemoryDb.AzureCosmosDBMongoDB;

public class AzureCosmosDBMongoDBMemoryException : KernelMemoryException
{
    /// <inheritdoc />
    public AzureCosmosDBMongoDBMemoryException()
    {
    }

    /// <inheritdoc />
    public AzureCosmosDBMongoDBMemoryException(string? message) : base(message)
    {
    }

    /// <inheritdoc />
    public AzureCosmosDBMongoDBMemoryException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
