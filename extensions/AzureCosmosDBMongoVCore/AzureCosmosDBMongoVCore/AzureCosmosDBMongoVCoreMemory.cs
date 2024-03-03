// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Configuration;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryStorage;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Microsoft.KernelMemory.MemoryDb.AzureCosmosDBMongoVCore;

/// <summary>
/// Azure CosmosDB Mongo vCore connector for Kernel Memory
/// </summary>

public class AzureCosmosDBMongoVCoreMemory : IMemoryDb
{
    private readonly ITextEmbeddingGenerator _embeddingGenerator;
    private readonly ILogger<AzureCosmosDBMongoVCoreMemory> _log;
    private readonly AzureCosmosDBMongoVCoreConfig _config;
    private readonly IMongoClient _cosmosDBMongoClient;
    private readonly IMongoDatabase _cosmosMongoDatabase;
    private readonly IMongoCollection<AzureCosmosDBMongoVCoreMemoryRecord> _cosmosMongoCollection;
    private readonly String _collectionName;

    /// <summary>
    /// Create a new instance
    /// </summary>
    /// <param name="config"> Azure Cosmos DB Mongo vCore configuration</parama>
    /// <param name="embeddingGenerator">Text embedding generator</param>
    /// <param name="log">Application logger</param>
    public AzureCosmosDBMongoVCoreMemory(
        AzureCosmosDBMongoVCoreConfig config,
        ITextEmbeddingGenerator embeddingGenerator,
        String databaseName,
        String collectionName,
        ILogger<AzureCosmosDBMongoVCoreMemory>? log = null) 
    {
            this._embeddingGenerator = embeddingGenerator;
            this._log = log ?? DefaultLogger<AzureCosmosDBMongoVCoreMemory>.Instance;
            this._config = config;

            if (string.IsNullOrEmpty(config.ConnectionString)) {
                this._log.LogCritical("Azure Cosmos DB Mongo vCore connection string is empty.");
                throw new AzureCosmosDBMongoVCoreMemoryException("Azure Cosmos DB Mongo vCore connection string is empty.");
            }

            if (this._embeddingGenerator == null) {
                throw new AzureCosmosDBMongoVCoreMemoryException("Embedding generator not configured");
            }

            this._cosmosDBMongoClient = new MongoClient(config.ConnectionString);
            this._cosmosMongoDatabase = this._cosmosDBMongoClient.GetDatabase(databaseName);

            this._cosmosMongoDatabase.CreateCollectionAsync(collectionName);
            this._cosmosMongoCollection = this._cosmosMongoDatabase.GetCollection<AzureCosmosDBMongoVCoreMemoryRecord>(collectionName);
            this._collectionName = collectionName;
    }

    /// <inheritdoc />
    public async Task CreateIndexAsync(string indexName, int vectorSize, CancellationToken cancellationToken = default)
    {   
        var indexes = await this._cosmosMongoCollection.Indexes.ListAsync().ConfigureAwait(false);
        if (!indexes.ToList().Any(index => index["name"] == indexName)) {
            var command = new BsonDocument();
            switch (this._config.Kind)
            {
                case "vector-ivf":
                    command = GetIndexDefinitionVectorIVF(indexName, vectorSize);
                    break;
                case "vector-hnsw":
                    command = GetIndexDefinitionVectorHNSW(indexName, vectorSize);
                    break;    
            }

            await this._cosmosMongoDatabase.RunCommandAsync<BsonDocument>(command).ConfigureAwait(false);
        }
    }

    private BsonDocument GetIndexDefinitionVectorIVF(string index, int vectorSize)
    {
        return new BsonDocument
            {
                { "createIndexes", this._collectionName },
                {
                    "indexes",
                    new BsonArray
                    {
                        new BsonDocument
                        {
                            { "name", index },
                            { "key", new BsonDocument { { "embedding", "cosmosSearch" } } },
                            { "cosmosSearchOptions", new BsonDocument
                                {
                                    { "kind", this._config.Kind },
                                    { "numLists", this._config.NumLists },
                                    { "similarity", this._config.Similarity },
                                    { "dimensions", vectorSize}
                                }
                            }
                        }
                    }
                }
            };
    }

    private BsonDocument GetIndexDefinitionVectorHNSW(string index, int vectorSize)
    {
        return new BsonDocument
        {
            { "createIndexes", this._collectionName },
            {
                "indexes",
                new BsonArray
                {
                    new BsonDocument
                    {
                        { "name", index },
                        { "key", new BsonDocument { { "embedding", "cosmosSearch" } } },
                        { "cosmosSearchOptions", new BsonDocument
                            {
                                { "kind", this._config.Kind },
                                { "m", this._config.NumberOfConnections },
                                { "efConstruction", this._config.EfConstruction},
                                { "similarity", this._config.Similarity },
                                { "dimensions", vectorSize}
                            }
                        }
                    }
                }
            }
        }; 
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetIndexesAsync(CancellationToken cancellationToken = default)
    {
        var indexesCursor = await this._cosmosMongoCollection.Indexes.ListAsync(cancellationToken).ConfigureAwait(false);
        var indexes = await indexesCursor.ToListAsync(cancellationToken).ConfigureAwait(false);

        return indexes.Select(index => index["name"].AsString);
    }

    /// <inheritdoc />
    public async Task DeleteIndexAsync(string index, CancellationToken cancellationToken = default)
    {
        await this._cosmosMongoCollection.Indexes.DropOneAsync(index).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> UpsertAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        AzureCosmosDBMongoVCoreMemoryRecord localRecord = AzureCosmosDBMongoVCoreMemoryRecord.FromMemoryRecord(record);
        var filter = Builders<AzureCosmosDBMongoVCoreMemoryRecord>.Filter.Eq(r => r.Id, localRecord.Id);

        var replaceOptions = new ReplaceOptions() { IsUpsert = true };

        await _cosmosMongoCollection.ReplaceOneAsync(filter, localRecord, replaceOptions, cancellationToken).ConfigureAwait(false);

        return record.Id;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<(MemoryRecord, double)> GetSimilarListAsync(
        string index,
        string text,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        int limit = 1,
        bool withEmbeddings = false,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0) { limit=int.MaxValue;}

        Embedding embedding = await this._embeddingGenerator.GenerateEmbeddingAsync(text, cancellationToken).ConfigureAwait(false);

        BsonDocument[] pipeline = null;
        switch (this._config.Kind)
        {
            case "vector-ivf":
                pipeline = GetVectorIVFSearchPipeline(embedding, limit);
                break;
            case "vector-hnsw":
                pipeline = GetVectorHNSWSearchPipeline(embedding, limit);
                break;    
        }

        using var cursor = await this._cosmosMongoCollection.AggregateAsync<BsonDocument>(pipeline).ConfigureAwait(false);    
        while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var doc in cursor.Current)
            {
                // Access the similarityScore from the BSON document
                var similarityScore = doc.GetValue("similarityScore").AsDouble;
                if (similarityScore < minRelevance) { continue; }

                MemoryRecord memoryRecord = AzureCosmosDBMongoVCoreMemoryRecord.ToMemoryRecord(doc, withEmbeddings);

                yield return (memoryRecord, similarityScore);
            }
        }
    }

    private BsonDocument[] GetVectorIVFSearchPipeline(Embedding embedding, int limit)
    {
        string searchStage = @"
        {
            ""$search"": {
                ""cosmosSearch"": {
                    ""vector"": [" + string.Join(",",  embedding.Data.ToArray().Select(f => f.ToString())) + @"],
                    ""path"": ""embedding"",
                    ""k"": " + limit + @"
                },
                ""returnStoredSource"": true
            }
        }";

        string projectStage = @"
        {
            ""$project"": {
                ""similarityScore"": { ""$meta"": ""searchScore"" },
                ""document"": ""$$ROOT""
            }
        }";

        BsonDocument searchBson = BsonDocument.Parse(searchStage);
        BsonDocument projectBson = BsonDocument.Parse(projectStage);
        return new BsonDocument[]
        {
            searchBson, projectBson
        };
    }

    private BsonDocument[] GetVectorHNSWSearchPipeline(Embedding embedding, int limit)
    {
        string searchStage = @"
        {
            ""$search"": {
                ""cosmosSearch"": {
                    ""vector"": [" + string.Join(",", embedding.Data.ToArray().Select(f => f.ToString())) + @"],
                    ""path"": ""embedding"",
                    ""k"": " + limit + @",
                    ""efSearch"": " + this._config.EfSearch + @"
                }
            }
        }";

        string projectStage = @"
        {
            ""$project"": {
                ""similarityScore"": { ""$meta"": ""searchScore"" },
                ""document"": ""$$ROOT""
            }
        }";

        BsonDocument searchBson = BsonDocument.Parse(searchStage);
        BsonDocument projectBson = BsonDocument.Parse(projectStage);
        return new BsonDocument[]
        {
            searchBson, projectBson
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<MemoryRecord> GetListAsync(
        string index,
        ICollection<MemoryFilter>? filters = null,
        int limit = 1,
        bool withEmbeddings = false,
        CancellationToken cancellationToken = default)
    {
        // TODO: Add logic for search with filters, once it is released in MongoDB vCore.
        // For now this method returns an empty list.
        yield break;
    }    

    public async Task DeleteAsync(
        string index, 
        MemoryRecord record,
        CancellationToken cancellationToken = default)
    {
        AzureCosmosDBMongoVCoreMemoryRecord localRecord = AzureCosmosDBMongoVCoreMemoryRecord.FromMemoryRecord(record);
        var filter = Builders<AzureCosmosDBMongoVCoreMemoryRecord>.Filter.Eq(r => r.Id, localRecord.Id);
        await _cosmosMongoCollection.DeleteOneAsync(filter, cancellationToken).ConfigureAwait(false);
    }
}

