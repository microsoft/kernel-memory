// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.SemanticMemory.Diagnostics;

public class OrchestrationException : SemanticMemoryException
{
    /// <inheritdoc />
    public OrchestrationException() { }

    /// <inheritdoc />
    public OrchestrationException(string message) : base(message) { }

    /// <inheritdoc />
    public OrchestrationException(string message, Exception? innerException) : base(message, innerException) { }
}
