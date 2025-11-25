// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory.MemoryDb.Elasticsearch;

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
