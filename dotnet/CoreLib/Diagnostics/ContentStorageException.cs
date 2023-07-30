// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.SemanticMemory.Core20;

namespace Microsoft.SemanticMemory.Core.Diagnostics;

public class ContentStorageException : SemanticMemoryException
{
    /// <inheritdoc />
    public ContentStorageException() { }

    /// <inheritdoc />
    public ContentStorageException(string message) : base(message) { }

    /// <inheritdoc />
    public ContentStorageException(string message, Exception? innerException) : base(message, innerException) { }
}
