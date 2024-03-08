using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System.Collections.Generic;

namespace Microsoft.KernelMemory.MongoDbAtlas;

public class MongoDbKernelMemoryBaseStorage
{
    protected IMongoDatabase Database { get; private set; }

    protected MongoDbAtlasConfig Config { get; private set; }

    /// <summary>
    /// Keys are mongo collection of T but since we do not know T we cache them
    /// as simple object then cast to the correct value.
    /// </summary>
    protected Dictionary<string, object> Collections { get; private set; } = new();

    public MongoDbKernelMemoryBaseStorage(MongoDbAtlasConfig config)
    {
        this.Database = config.GetDatabase();
        this.Config = config;
    }

    protected IMongoCollection<BsonDocument> GetCollection(string collectionName)
    {
        return this.GetCollection<BsonDocument>(collectionName);
    }

    protected GridFSBucket<string> GetBucketForIndex(string indexName)
    {
        return new GridFSBucket<string>(this.Database,
            new GridFSBucketOptions()
            {
                BucketName = indexName
            });
    }

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
