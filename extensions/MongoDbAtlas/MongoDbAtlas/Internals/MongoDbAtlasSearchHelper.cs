// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Microsoft.KernelMemory.MongoDbAtlas;

/// <summary>
/// <para>Wrapper for ATLAS search indexes stuff</para>
/// <para>
/// <ul>
/// <li>https://www.mongodb.com/docs/v7.0/reference/method/db.collection.createSearchIndex/</li>
/// <li>Normalizer (end of the page) https://www.mongodb.com/docs/atlas/atlas-search/analyzers/</li>
/// </ul>
/// </para>
/// </summary>
internal sealed class MongoDbAtlasSearchHelper
{
    private readonly IMongoDatabase _db;

    /// <summary>
    /// Construct helper to interact with atlas and create mappings etc.
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="dbName"></param>
    public MongoDbAtlasSearchHelper(string connection, string dbName)
    {
        var client = MongoDbAtlasDatabaseHelper.GetClient(connection);
        this._db = client.GetDatabase(dbName);
    }

    /// <summary>
    /// Get the name of the index to perform a $search aggregation
    /// </summary>
    /// <param name="collectionName"></param>
    /// <returns></returns>
    public string GetIndexName(string collectionName) => $"searchix_{collectionName}";

    /// <summary>
    /// Create an ATLAS index and return the id of the index. It also wait for the index to be
    /// ready, and create the collection if needed.
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="embeddingDimension"></param>
    /// <returns></returns>
    public async Task<IndexInfo> CreateIndexAsync(string collectionName, int embeddingDimension)
    {
        //I need to be able to create index even if collection does not exists
        //if collection does not exists, create collection and index
        //if collection does not exists, index does not exists
        if (!await this.CollectionExistsAsync(collectionName).ConfigureAwait(false))
        {
            //index does not exists because collection does not exists
            await this._db.CreateCollectionAsync(collectionName).ConfigureAwait(false);
        }

        var status = await this.GetIndexInfoAsync(collectionName).ConfigureAwait(false);
        if (status.Exists)
        {
            //index exists, but we found that status is "does_not_exist" this identify a stale state.
            if (status.Status == "does_not_exist")
            {
                //delete the index and recreate it.
                await this.DeleteIndicesAsync(collectionName).ConfigureAwait(false);
            }
            else
            {
                return status;
            }
        }

        //now I can create the index.
        var command = this.CreateCreationCommand(collectionName, embeddingDimension);
        var result = await this._db.RunCommandAsync<BsonDocument>(command).ConfigureAwait(false);
        var creationResult = result["indexesCreated"] as BsonArray;
        if (creationResult == null || creationResult.Count == 0)
        {
            return s_falseIndexInfo;
        }

        return await this.GetIndexInfoAsync(collectionName).ConfigureAwait(false);
    }

    /// <summary>
    /// Delete all the indices for a specific collection
    /// </summary>
    /// <param name="collectionName"></param>
    public async Task DeleteIndicesAsync(string collectionName)
    {
        var pipeline = new BsonDocument[]
        {
            new()
            {
                {
                    "$listSearchIndexes",
                    new BsonDocument()
                }
            }
        };

        var collection = this._db.GetCollection<BsonDocument>(collectionName);
        var result = await collection.AggregateAsync<BsonDocument>(pipeline).ConfigureAwait(false);
        var allIndexInfo = await result.ToListAsync().ConfigureAwait(false);

        //for each index we need to delete the indices.
        foreach (var index in allIndexInfo)
        {
            var id = index["id"].AsString;

            var command = new BsonDocument
            {
                { "dropSearchIndex", collectionName },
                { "id", id }
            };
            await this._db.RunCommandAsync<BsonDocument>(command).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Simply wait for the index to be ready for a specific collection
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="secondsToWait"></param>
    public async Task WaitForIndexToBeReadyAsync(string collectionName, int secondsToWait)
    {
        //cycle for max 10 seconds to wait for index to be ready
        var maxWait = DateTime.UtcNow.AddSeconds(secondsToWait);
        while (DateTime.UtcNow < maxWait)
        {
            var indexInfo = await this.GetIndexInfoAsync(collectionName).ConfigureAwait(false);
            if (indexInfo.Exists && indexInfo.Queryable)
            {
                return;
            }

            await Task.Delay(100).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// https://www.mongodb.com/docs/upcoming/reference/command/createSearchIndexes/
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="embeddingDimension"></param>
    /// <returns></returns>
    public BsonDocument CreateCreationCommand(string collectionName, int embeddingDimension)
    {
        return new BsonDocument
        {
            { "createSearchIndexes", collectionName },
            {
                "indexes", new BsonArray
                {
                    new BsonDocument
                    {
                        { "name", this.GetIndexName(collectionName) },
                        { "type", "vectorSearch" },
                        {
                            "definition", new BsonDocument
                            {
                                {
                                    "fields", new BsonArray
                                    {
                                        new BsonDocument
                                        {
                                            { "path", "Embedding" },
                                            { "type", "vector" },
                                            { "numDimensions", embeddingDimension },
                                            { "similarity", "cosine" }
                                        },
                                        new BsonDocument
                                        {
                                            { "type", "filter" },
                                            { "path", "Index" }
                                        },
                                        new BsonDocument
                                        {
                                            { "type", "filter" },
                                            { "path", "Tags.Key" }
                                        },
                                        new BsonDocument
                                        {
                                            { "type", "filter" },
                                            { "path", "Tags.Values" }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    /// <summary>
    /// Utility function to drop the entire database with all search indexes created.
    /// </summary>
    /// <returns></returns>
    public async Task DropDatabaseAsync()
    {
        //enumerate all collection
        var collections = await this._db.ListCollectionsAsync().ConfigureAwait(false);
        var collectionsName = await collections.ToListAsync().ConfigureAwait(false);
        foreach (var collection in collectionsName.Select(b => b["name"].AsString))
        {
            await this.DeleteIndicesAsync(collection).ConfigureAwait(false);
            await this._db.DropCollectionAsync(collection).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Utility function to delete all documents from all collections but leave search index
    /// it is useful for tests.
    /// </summary>
    /// <returns></returns>
    public async Task DropAllDocumentsFromCollectionsAsync()
    {
        var collections = await this._db.ListCollectionsAsync().ConfigureAwait(false);
        var collectionsName = await collections.ToListAsync().ConfigureAwait(false);
        foreach (var collectionName in collectionsName.Select(b => b["name"].AsString))
        {
            var collection = this._db.GetCollection<BsonDocument>(collectionName);
            //delete all documents
            await collection.DeleteManyAsync(new BsonDocument()).ConfigureAwait(false);
        }
    }

    public sealed record IndexInfo(bool Exists, string Status, Boolean Queryable);

    /// <summary>
    /// Verify CreateIndexSettingsAnalysisDescriptor for mapper 7
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private BsonArray GetAnalyzersList()
    {
        var analyzers = new BsonArray();

        return analyzers;
    }

    private async Task<bool> CollectionExistsAsync(string connectionName)
    {
        var filter = new BsonDocument("name", connectionName);
        var collections = await this._db.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter }).ConfigureAwait(false);
        return collections.Any();
    }

    /// <summary>
    /// Retrieve information about an MongoDB Atlas index for a specific
    /// collection name. If the index does not exists it returns null
    /// </summary>
    private async Task<IndexInfo> GetIndexInfoAsync(string collectionName)
    {
        var collection = this._db.GetCollection<BsonDocument>(collectionName);
        var pipeline = new BsonDocument[]
        {
            new()
            {
                {
                    "$listSearchIndexes",
                    new BsonDocument
                    {
                        { "name", this.GetIndexName(collectionName) }
                    }
                }
            }
        };

        //if collection does not exists, index does not exists
        if (!await this.CollectionExistsAsync(collectionName).ConfigureAwait(false))
        {
            //index does not exists because collection does not exists
            return s_falseIndexInfo;
        }

        var result = await collection.AggregateAsync<BsonDocument>(pipeline).ConfigureAwait(false);
        var allIndexInfo = await result.ToListAsync().ConfigureAwait(false);

        //Verify if we have information about the index.
        if (allIndexInfo.Count == 0)
        {
            return s_falseIndexInfo;
        }

        if (allIndexInfo.Count > 1)
        {
            throw new MongoDbAtlasException("We have too many atlas search index for the collection: " + string.Join(",", allIndexInfo.Select(i => i["name"].AsString)));
        }

        var indexInfo = allIndexInfo[0];
        var status = indexInfo["status"]!.AsString!.ToLower(System.Globalization.CultureInfo.InvariantCulture);
        return new IndexInfo(true, status, indexInfo["queryable"].AsBoolean);
    }

    private static readonly IndexInfo s_falseIndexInfo = new(false, "", false);
}
