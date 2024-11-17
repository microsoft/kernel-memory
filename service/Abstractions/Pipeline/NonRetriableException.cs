// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.KernelMemory.Pipeline;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

public class NonRetriableException : OrchestrationException
{
    /// <inheritdoc />
    public NonRetriableException() { }

    /// <inheritdoc />
    public NonRetriableException(string message) : base(message) { }

    /// <inheritdoc />
    public NonRetriableException(string message, Exception? innerException) : base(message, innerException) { }
}
