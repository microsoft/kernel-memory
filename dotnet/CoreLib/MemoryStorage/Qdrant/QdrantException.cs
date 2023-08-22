// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.SemanticMemory.MemoryStorage.Qdrant;

public class QdrantException : SemanticMemoryException
{
    /// <inheritdoc />
    public QdrantException() { }

    /// <inheritdoc />
    public QdrantException(string message) : base(message) { }

    /// <inheritdoc />
    public QdrantException(string message, Exception? innerException) : base(message, innerException) { }
}
