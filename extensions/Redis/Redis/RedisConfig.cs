// Copyright (c) Microsoft. All rights reserved.

using NRedisStack.Search;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// Lays out the tag fields that you want redis to index.
/// </summary>
public class RedisConfig
{
    /// <summary>
    /// The default prefix to be used for index names.
    /// </summary>
    public const string DefaultIndexPrefix = "km-";

    /// <summary>
    /// Connection string required to connect to Redis
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets the Prefix to use for prefix index names and all documents
    /// inserted into Redis as part of Kernel Memory's operations.
    /// </summary>
    public string AppPrefix { get; }

    /// <summary>
    /// Gets or sets the Vector Algorithm to use. Defaults to HNSW.
    /// </summary>
    public Schema.VectorField.VectorAlgo VectorAlgorithm { get; set; } = Schema.VectorField.VectorAlgo.HNSW;

    /// <summary>
    /// The Collection of tags that you want to be able to search on.
    /// The Key is the tag name, and the char is the separator that you
    /// want Redis to use to separate your tag fields. The default separator
    /// is ','.
    /// </summary>
    public Dictionary<string, char?> Tags { get; } = new()
    {
        { Constants.ReservedDocumentIdTag, '|' },
        { Constants.ReservedFileIdTag, '|' },
        { Constants.ReservedFilePartitionTag, '|' },
        { Constants.ReservedFileSectionNumberTag, '|' },
        { Constants.ReservedFileTypeTag, '|' },
    };

    /// <summary>
    /// Initializes an instance of RedisMemoryConfiguration.
    /// </summary>
    /// <param name="appPrefix">The prefix to use for the index name and all documents inserted into Redis.</param>
    /// <param name="tags">The collection of tags you want to be able to search on. The key</param>
    public RedisConfig(string appPrefix = DefaultIndexPrefix, Dictionary<string, char?>? tags = null)
    {
        this.AppPrefix = appPrefix;

        if (tags is not null)
        {
            foreach (var tag in tags)
            {
                this.Tags[tag.Key] = tag.Value;
            }
        }
    }
}
