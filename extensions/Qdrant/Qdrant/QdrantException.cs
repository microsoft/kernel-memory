// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.MemoryDb.Qdrant;

public sealed class QdrantException : KernelMemoryException
{
    /// <inheritdoc />
    public QdrantException() { }

    /// <inheritdoc />
    public QdrantException(string message) : base(message) { }

    /// <inheritdoc />
    public QdrantException(string message, Exception? innerException) : base(message, innerException) { }
}
