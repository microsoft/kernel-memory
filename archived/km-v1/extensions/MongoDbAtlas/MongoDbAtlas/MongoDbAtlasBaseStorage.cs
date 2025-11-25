// Copyright (c) Microsoft. All rights reserved.

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.KernelMemory.MongoDbAtlas;

/// <summary>
/// Base storage class for both memory and vector storage classes
/// </summary>
[Experimental("KMEXP03")]
public abstract class MongoDbAtlasBaseStorage
{
    /// <summary>
    /// Database instance
    /// </summary>
    protected IMongoDatabase Database { get; private set; }

    /// <summary>
    /// Configuration instance
    /// </summary>
    protected MongoDbAtlasConfig Config { get; private set; }

    /// <summary>
    /// Keys are mongo collection of T but since we do not know T we cache them
    /// as simple object then cast to the correct value.
    /// </summary>
    private Dictionary<string, object> Collections { get; set; } = [];

    /// <summary>
    /// Create an instance of the storage based on configuration
    /// </summary>
    /// <param name="config"></param>
    protected MongoDbAtlasBaseStorage(MongoDbAtlasConfig config)
    {
        this.Database = MongoDbAtlasDatabaseHelper.GetDatabase(config.ConnectionString, config.DatabaseName);
        this.Config = config;
    }

    /// <summary>
    /// Get an instance of the collection given the collection name
    /// </summary>
    /// <param name="collectionName"></param>
    /// <returns></returns>
    protected IMongoCollection<BsonDocument> GetCollection(string collectionName)
    {
        return this.GetCollection<BsonDocument>(collectionName);
    }

    /// <summary>
    /// Get a reference to a GridFS bucket for a specific index. Remember that each
    /// index has a different bucket.
    /// </summary>
    /// <param name="indexName"></param>
    /// <returns></returns>
    protected GridFSBucket<string> GetBucketForIndex(string indexName)
    {
        return new GridFSBucket<string>(this.Database,
            new GridFSBucketOptions()
            {
                BucketName = indexName
            });
    }

    /// <summary>
    /// Get a typed collection given the collection name, it uses a local cache to avoid
    /// recreating the collection instance each call.
    /// </summary>
    /// <param name="collectionName"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    protected IMongoCollection<T> GetCollection<T>(string collectionName)
    {
        if (!this.Collections.TryGetValue(collectionName, out object? value))
        {
            value = this.Database.GetCollection<T>(collectionName);
            this.Collections[collectionName] = value;
        }

        return (IMongoCollection<T>)value;
    }
}
