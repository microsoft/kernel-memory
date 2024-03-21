// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.MemoryDb.Chroma;

/// <summary>
/// Collection not found exception
/// </summary>
public class ChromaCollectionNotFoundException : ChromaException
{
    /// <inheritdoc />
    public ChromaCollectionNotFoundException() { }

    /// <inheritdoc />
    public ChromaCollectionNotFoundException(string message) : base(message) { }

    /// <inheritdoc />
    public ChromaCollectionNotFoundException(string message, Exception? innerException) : base(message, innerException) { }
}
