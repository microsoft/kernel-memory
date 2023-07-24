// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.SemanticKernel.SemanticMemory.Core.Diagnostics;

public class OrchestrationException : MemoryException
{
    /// <inheritdoc />
    public OrchestrationException() { }

    /// <inheritdoc />
    public OrchestrationException(string message) : base(message) { }

    /// <inheritdoc />
    public OrchestrationException(string message, Exception? innerException) : base(message, innerException) { }
}
