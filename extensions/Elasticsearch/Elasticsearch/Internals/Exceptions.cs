// Copyright (c) Free Mind Labs, Inc. All rights reserved.


// Copyright (c) Free Mind Labs, Inc. All rights reserved.

using Microsoft.KernelMemory;

namespace Microsoft.KernelMemory.MemoryDb.Elasticsearch;

/// <summary>
/// Exception thrown when the Elasticsearch configuration is invalid in appSettings, secrets, etc.
/// </summary>
public class Exceptions : ElasticsearchException
{
    /// <inheritdoc />
    public Exceptions() { }

    /// <inheritdoc />
    public Exceptions(string message) : base(message) { }

    /// <inheritdoc />
    public Exceptions(string message, Exception? innerException) : base(message, innerException) { }
}

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

/// <summary>
/// Exception thrown when an index name does pass Elasticsearch validation.
/// </summary>
public class InvalidIndexNameException : ElasticsearchException
{
    /// <inheritdoc/>
    public InvalidIndexNameException(string indexName, IEnumerable<string> errors, Exception? innerException = default)
        : base($"The given index name '{indexName}' is invalid. {string.Join(", ", errors)}", innerException)
    {
        this.IndexName = indexName;
        this.Errors = errors;
    }

    /// <inheritdoc/>
    public InvalidIndexNameException(
        (string IndexName, IEnumerable<string> Errors) conversionResult,
        Exception? innerException = default)

        => (this.IndexName, this.Errors) = conversionResult;

    /// <summary>
    /// The index name that failed validation.
    /// </summary>
    public string IndexName { get; }

    /// <summary>
    /// The list of errors that caused the validation to fail.
    /// </summary>
    public IEnumerable<string> Errors { get; }
}
