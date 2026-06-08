// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.Pipeline;

public class MimeTypeException : KernelMemoryException
{
    /// <inheritdoc />
    public MimeTypeException(bool? isTransient = null)
    {
        this.IsTransient = isTransient;
    }

    /// <inheritdoc />
    public MimeTypeException(string message, bool? isTransient = null) : base(message)
    {
        this.IsTransient = isTransient;
    }

    /// <inheritdoc />
    public MimeTypeException(string message, Exception? innerException, bool? isTransient = null) : base(message, innerException)
    {
        this.IsTransient = isTransient;
    }
}
