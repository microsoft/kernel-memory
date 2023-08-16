// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.SemanticMemory.Client;

namespace Microsoft.SemanticMemory.Core.MemoryStorage.Qdrant;

public class QdrantException : SemanticMemoryException
{
    /// <inheritdoc />
    public QdrantException() { }

    /// <inheritdoc />
    public QdrantException(string message) : base(message) { }

    /// <inheritdoc />
    public QdrantException(string message, Exception? innerException) : base(message, innerException) { }
}
