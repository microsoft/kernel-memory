// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.MemoryStorage.DevTools;

public class SimpleVectorDbException : KernelMemoryException
{
    /// <inheritdoc />
    public SimpleVectorDbException() { }

    /// <inheritdoc />
    public SimpleVectorDbException(string message) : base(message) { }

    /// <inheritdoc />
    public SimpleVectorDbException(string message, Exception? innerException) : base(message, innerException) { }
}
