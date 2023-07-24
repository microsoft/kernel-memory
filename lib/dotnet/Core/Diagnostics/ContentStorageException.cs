// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.SemanticKernel.SemanticMemory.Core.Diagnostics;

public class ContentStorageException : MemoryException
{
    /// <inheritdoc />
    public ContentStorageException() { }

    /// <inheritdoc />
    public ContentStorageException(string message) : base(message) { }

    /// <inheritdoc />
    public ContentStorageException(string message, Exception? innerException) : base(message, innerException) { }
}
