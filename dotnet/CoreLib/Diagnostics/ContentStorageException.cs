// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.SemanticMemory.Diagnostics;

public class ContentStorageException : SemanticMemoryException
{
    /// <inheritdoc />
    public ContentStorageException() { }

    /// <inheritdoc />
    public ContentStorageException(string message) : base(message) { }

    /// <inheritdoc />
    public ContentStorageException(string message, Exception? innerException) : base(message, innerException) { }
}

public class ContentStorageFileNotFoundException : ContentStorageException
{
    /// <inheritdoc />
    public ContentStorageFileNotFoundException() { }

    /// <inheritdoc />
    public ContentStorageFileNotFoundException(string message) : base(message) { }

    /// <inheritdoc />
    public ContentStorageFileNotFoundException(string message, Exception? innerException) : base(message, innerException) { }
}
