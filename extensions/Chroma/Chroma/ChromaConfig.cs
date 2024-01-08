// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.Configuration;

namespace Microsoft.KernelMemory.MemoryDb.Chroma;

/// <summary>
/// Chroma configuration
/// </summary>
public class ChromaConfig
{
    /// <summary>
    /// Default prefix used for collection names
    /// </summary>
    public const string DefaultCollectionNamePrefix = "km-";

    /// <summary>
    /// Connection string required to connect to Postgres
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Mandatory prefix to add to collections created by KM.
    /// This is used to distinguish KM collections from others.
    /// </summary>
    /// <remarks>Default value is set to "km-" but can be override when creating a config.</remarks>
    public string CollectionNamePrefix { get; set; } = DefaultCollectionNamePrefix;

    /// <summary>
    /// Verify that the current state is valid.
    /// </summary>
    public void Validate()
    {
        // ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        this.CollectionNamePrefix = this.CollectionNamePrefix?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(this.CollectionNamePrefix))
        {
            throw new ConfigurationException("The collection name prefix is empty.");
        }
    }
}
