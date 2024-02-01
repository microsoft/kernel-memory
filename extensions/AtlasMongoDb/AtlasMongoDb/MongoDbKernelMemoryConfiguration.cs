using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace Alkampfer.KernelMemory.AtlasMongoDb;

public class MongoDbKernelMemoryConfiguration
{
    public string ConnectionString { get; set; } = null!;

    public string DatabaseName { get; set; } = null!;

    public bool UseSingleCollectionForVectorSearch { get; set; } = false;

    /// <summary>
    /// A callback useful for tests, we need to wait that the index really indexed data after
    /// we inserted into the collection so we need to wait a little bit.
    /// </summary>
    public Func<Task> AfterIndexCallbackAsync { get; private set; } = () => Task.CompletedTask;

    public MongoDbKernelMemoryConfiguration WithConnection(string mongoConnection)
    {
        this.ConnectionString = mongoConnection;
        return this;
    }

    public MongoDbKernelMemoryConfiguration WithDatabaseName(string databaseName)
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
    public MongoDbKernelMemoryConfiguration WithSingleCollectionForVectorSearch(bool useSingleCollectionForVectorSearch)
    {
        this.UseSingleCollectionForVectorSearch = useSingleCollectionForVectorSearch;
        return this;
    }

    public MongoDbKernelMemoryConfiguration WithAfterIndexCallback(Func<Task> afterIndexCallback)
    {
        this.AfterIndexCallbackAsync = afterIndexCallback;
        return this;
    }

    private MongoClient _client = null!;

    internal IMongoDatabase GetDatabase()
    {
        if (this._client == null)
        {
            var builder = new MongoUrlBuilder(this.ConnectionString);
            if (!String.IsNullOrEmpty(this.DatabaseName))
            {
                builder.DatabaseName = this.DatabaseName;
            }

            this._client = new MongoClient(builder.ToMongoUrl());
        }

        return this._client.GetDatabase(this.DatabaseName);
    }
}
