// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.MemoryStorage;

public class IndexNotFoundException : KernelMemoryException
{
    /// <inheritdoc />
    public IndexNotFoundException() { }

    /// <inheritdoc />
    public IndexNotFoundException(string message) : base(message) { }

    /// <inheritdoc />
    public IndexNotFoundException(string message, Exception? innerException) : base(message, innerException) { }
}
