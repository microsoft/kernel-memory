// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.DataFormats;

public class UnsupportedContentException : KernelMemoryException
{
    /// <inheritdoc />
    public UnsupportedContentException()
    {
    }

    /// <inheritdoc />
    public UnsupportedContentException(string? message) : base(message)
    {
    }

    /// <inheritdoc />
    public UnsupportedContentException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
