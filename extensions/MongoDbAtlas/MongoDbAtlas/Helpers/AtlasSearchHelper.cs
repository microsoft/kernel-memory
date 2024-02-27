// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Microsoft.KernelMemory.MongoDbAtlas.Helpers;

/// <summary>
/// <para>Wrapper for ATLAS search indexes stuff</para>
/// <para>
/// <ul>
/// <li>https://www.mongodb.com/docs/v7.0/reference/method/db.collection.createSearchIndex/</li>
/// <li>Normalizer (end of the page) https://www.mongodb.com/docs/atlas/atlas-search/analyzers/</li>
/// </ul>
/// </para>
/// </summary>
internal sealed class AtlasSearchHelper
{
    private readonly IMongoDatabase _db;

    /// <summary>
    /// Construct helper to interact with atlas and create mappings etc.
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="dbName"></param>
    public AtlasSearchHelper(string connection, string dbName)
    {
        var client = new MongoClient(connection);
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
            return status;
        }

        //now I can create the index.
        var command = this.CreateCreationCommand(collectionName, embeddingDimension);
        var result = await this._db.RunCommandAsync<BsonDocument>(command).ConfigureAwait(false);
        var creationResult = result["indexesCreated"] as BsonArray;
        if (creationResult.Count == 0)
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
            new BsonDocument
            {
                {
                    "$listSearchIndexes",
                    new BsonDocument
                    {
                    }
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
            if (indexInfo.Exists && indexInfo.Status != "pending")
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
                        {
                            "definition", new BsonDocument
                            {
                                { "mappings", this.GetMappings(embeddingDimension) },
                                { "analyzers", this.GetAnalyzersList() },
                            }
                        }
                    }
                }
            }
        };
    }

    /// <summary>
    /// https://www.mongodb.com/docs/atlas/atlas-search/define-field-mappings/#std-label-fts-field-mappings
    /// Create the mappings for AtlasMongoDB
    /// </summary>
    /// <param name="numberOfDimensions"></param>
    /// <returns></returns>
    private BsonDocument GetMappings(int numberOfDimensions)
    {
        var mappings = new BsonDocument();

        mappings["dynamic"] = true;

        var fields = new BsonDocument();
        mappings["fields"] = fields;

        fields["embedding"] = new BsonDocument
        {
            { "type", "knnVector" },
            { "dimensions", numberOfDimensions },
            { "similarity", "cosine" }
        };

        return mappings;
    }

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

    /// <summary>
    /// Retrieve information about an Atlas MongoDb index for a specific
    /// collection name. If the index does not exists it returns null
    /// </summary>
    /// <param name="collectionName"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private async Task<IndexInfo> GetIndexInfoAsync(string collectionName)
    {
        var collection = this._db.GetCollection<BsonDocument>(collectionName);
        var pipeline = new BsonDocument[]
        {
            new BsonDocument
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
            throw new Exception("We have too many atlas search index for the collection: " + string.Join(",", allIndexInfo.Select(i => i["name"].AsString)));
        }

        var indexInfo = allIndexInfo[0];

        var latestDefinition = indexInfo["latestDefinition"] as BsonDocument;
        var mapping = latestDefinition["mappings"] as BsonDocument;

        var deserializedMapping = BsonSerializer.Deserialize<AtlasMapping>(mapping);

        return new IndexInfo(true, indexInfo["status"].AsString.ToLower(), deserializedMapping);
    }

    private static readonly IndexInfo s_falseIndexInfo = new IndexInfo(false, "", null);

    public record IndexInfo(bool Exists, string Status, AtlasMapping Mapping);
}

public class AtlasMapping
{
    [BsonElement("dynamic")]
    public bool Dynamic { get; set; }

    [BsonElement("fields")]
    public Dictionary<string, FieldProperties>? Fields { get; set; }
}

public class FieldProperties
{
    [BsonElement("type")]
    public string Type { get; set; }

    [BsonElement("analyzer")]
    public string Analyzer { get; set; }

    [BsonElement("store")]
    public bool Store { get; set; }

    [BsonElement("vector")]
    public VectorProperties Vector { get; set; }

    [BsonElement("multi")]
    public Dictionary<string, MultiProperties> Multi { get; set; }

    [BsonElement("dimensions")]
    public int Dimensions { get; set; }

    [BsonElement("similarity")]
    public string Similarity { get; set; }
}

public class VectorProperties
{
    [BsonElement("dimensions")]
    public int Dimensions { get; set; }

    [BsonElement("method")]
    public string Method { get; set; }

    [BsonElement("distance")]
    public string Distance { get; set; }

    [BsonElement("sparse")]
    public bool Sparse { get; set; }
}

public class MultiProperties
{
    [BsonElement("analyzer")]
    public string Analyzer { get; set; }

    [BsonElement("type")]
    public string Type { get; set; }
}
