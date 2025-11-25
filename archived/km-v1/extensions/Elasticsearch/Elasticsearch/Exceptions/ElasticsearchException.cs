// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory.MemoryDb.Elasticsearch;

/// <summary>
/// Base exception for all the exceptions thrown by the Elasticsearch connector for KernelMemory
/// </summary>
public class ElasticsearchException : KernelMemoryException
{
    /// <inheritdoc />
    public ElasticsearchException() { }

    /// <inheritdoc />
    public ElasticsearchException(string message) : base(message) { }

    /// <inheritdoc />
    public ElasticsearchException(string message, Exception? innerException) : base(message, innerException) { }
}
