// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.MemoryDb.Elasticsearch;

/// <summary>
/// Exception thrown when the Elasticsearch configuration is invalid in appSettings, secrets, etc.
/// </summary>
public class ConfigurationException : ElasticsearchException
{
    /// <inheritdoc />
    public ConfigurationException() { }

    /// <inheritdoc />
    public ConfigurationException(string message) : base(message) { }

    /// <inheritdoc />
    public ConfigurationException(string message, Exception? innerException) : base(message, innerException) { }
}
