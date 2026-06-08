// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.DocumentStorage;

public class DocumentStorageException : KernelMemoryException
{
    /// <inheritdoc />
    public DocumentStorageException() { }

    /// <inheritdoc />
    public DocumentStorageException(string message) : base(message) { }

    /// <inheritdoc />
    public DocumentStorageException(string message, Exception? innerException) : base(message, innerException) { }
}

public class DocumentStorageFileNotFoundException : DocumentStorageException
{
    /// <inheritdoc />
    public DocumentStorageFileNotFoundException() { }

    /// <inheritdoc />
    public DocumentStorageFileNotFoundException(string message) : base(message) { }

    /// <inheritdoc />
    public DocumentStorageFileNotFoundException(string message, Exception? innerException) : base(message, innerException) { }
}
