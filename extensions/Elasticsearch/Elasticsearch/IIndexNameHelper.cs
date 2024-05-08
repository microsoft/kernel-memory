// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.MemoryDb.Elasticsearch;

/// <summary>
/// A utility class to help with Elasticsearch index names.
/// It applies
/// </summary>
public interface IIndexNameHelper
{
    /// <summary>
    /// Attempts to convert the given index name to a valid Elasticsearch index name.
    /// </summary>
    /// <param name="indexName">The index name to convert.</param>
    /// <param name="result">The result of the conversion. The result includes the converted index name if the conversion succeeded, or a list of errors if the conversion failed.</param>
    /// <returns>A structure containing the actual index name or a list of errors if the conversion failed.</returns>
    /// <exception cref="ArgumentException"></exception>
    public bool TryConvert(string indexName, out (string ActualIndexName, IEnumerable<string> Errors) result);

    /// <summary>
    /// Converts the given index name to a valid Elasticsearch index name.
    /// It throws an exception if the conversion fails.
    /// </summary>
    /// <param name="indexName">The index name to convert.</param>
    /// <returns>The converted index name.</returns>
    public string Convert(string indexName);
}
