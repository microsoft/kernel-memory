// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.MemoryStorage;

public class IndexNotFound : KernelMemoryException
{
    /// <inheritdoc />
    public IndexNotFound() { }

    /// <inheritdoc />
    public IndexNotFound(string message) : base(message) { }

    /// <inheritdoc />
    public IndexNotFound(string message, Exception? innerException) : base(message, innerException) { }
}
