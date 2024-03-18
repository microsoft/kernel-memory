// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;

namespace Microsoft.KernelMemory.MongoDbAtlas;

/// <summary>
/// Represents configuration for MongoDB Atlas memory storage and vector storage.
/// </summary>
public class MongoDbAtlasConfig
{
    /// <summary>
    /// Full connection string to a valid instance of MongoDB Atlas. It can contain
    /// database name, but if <see cref="DatabaseName"/> is specified, it will override
    /// the value in this connection string.
    /// </summary>
    public string ConnectionString { get; set; } = null!;

    /// <summary>
    /// Allow to specify the database name, if not specified, it will be taken from the connection string.
    /// </summary>
    public string DatabaseName { get; set; } = null!;

    /// <summary>
    /// To reduce the number of indexes in Atlas, we can use a single collection for all the vectors.
    /// This option allows to use a single collection for all Kernel Memory indexes
    /// </summary>
    public bool UseSingleCollectionForVectorSearch { get; set; }

    /// <summary>
    /// A callback useful for tests, we need to wait that the index really indexed data after
    /// we inserted into the collection so we need to wait a little bit.
    /// </summary>
    public Func<Task> AfterIndexCallbackAsync { get; private set; } = () => Task.CompletedTask;

    /// <summary>
    /// Allows to specify connection string
    /// </summary>
    /// <param name="mongoConnection">Connection string</param>
    /// <returns></returns>
    public MongoDbAtlasConfig WithConnectionString(string mongoConnection)
    {
        this.ConnectionString = mongoConnection;
        return this;
    }

    /// <summary>
    /// Allows to specify database name.
    /// </summary>
    /// <param name="databaseName">Name of the database</param>
    /// <returns></returns>
    public MongoDbAtlasConfig WithDatabaseName(string databaseName)
    {
        this.DatabaseName = databaseName;
        return this;
    }

    /// <summary>
    /// If single collection for vector search is enabled, all the vectors will be stored in a single
    /// collection, so we have only one search index in atlas. This can be useful to reduce index numbers.
    /// </summary>
    /// <param name="useSingleCollectionForVectorSearch"></param>
    /// <returns></returns>
    public MongoDbAtlasConfig WithSingleCollectionForVectorSearch(bool useSingleCollectionForVectorSearch)
    {
        this.UseSingleCollectionForVectorSearch = useSingleCollectionForVectorSearch;
        return this;
    }

    /// <summary>
    /// Allow passing a callback that will be called after data is indexed.
    /// </summary>
    /// <param name="afterIndexCallback"></param>
    /// <returns></returns>
    public MongoDbAtlasConfig WithAfterIndexCallback(Func<Task> afterIndexCallback)
    {
        this.AfterIndexCallbackAsync = afterIndexCallback;
        return this;
    }
}
