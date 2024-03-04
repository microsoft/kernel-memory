// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.MongoDbAtlas;

internal class MongoDbAtlasKernelMemoryException : KernelMemoryException
{
    /// <inheritdoc />
    public MongoDbAtlasKernelMemoryException() { }

    /// <inheritdoc />
    public MongoDbAtlasKernelMemoryException(string message) : base(message) { }

    /// <inheritdoc />
    public MongoDbAtlasKernelMemoryException(string message, Exception? innerException) : base(message, innerException) { }
}
