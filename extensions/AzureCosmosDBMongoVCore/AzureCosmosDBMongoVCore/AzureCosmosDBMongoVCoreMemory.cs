// Copyright (c) Microsoft. All rights reserved.

using MongoClient;

namespace Microsoft.KernelMemory.MemoryDb.AzureCosmosDBMongoVCore;

/// <summary>
/// Azure CosmosDB Mongo vCore connector for Kernel Memory
/// </summary>

public class AzureCosmosDBMongoVCoreMemory : IMemoryDb
{
    private readonly ITextEmbeddingGenerator _embeddingGenerator;
    private readonly ILogger<AzureCosmosDBMongoVCoreMemory> _log;
    private readonly AzureCosmosDBConfig _config;
    private readonly IMongoClient _cosmosDBMongoClient;
    private readonly IMongoDatabase _cosmosMongoDatabase;
    private readonly IMongoCollection _cosmosMongoCollection;
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

            if (string.IsNullOrEmpty(config.GetConnectionString)) {
                this._log.LogCritical("Azure Cosmos DB Mongo vCore connection string is empty.");
                throw new ConfigurationException("Azure Cosmos DB Mongo vCore connection string is empty.");
            }

            if (this._embeddingGenerator == null) {
                throw new AzureCosmosDBMongoVCorehMemoryException("Embedding generator not configured");
            }

            this._cosmosDBMongoClient = new MongoClient(connectionString);
            this._cosmosMongoDatabase = this._cosmosDBMongoClient.GetDatabase(databaseName);
            await this._cosmosMongoDatabase.CreateCollectionAsync(collectionName);
            this._collectionName = collectionName
    }

    /// <inheritdoc />
    public Task CreateIndexAsync(string index, int vectorSize, CancellationToken cancellationToken = default)
    {
        if (!this._cosmosMongoDatabase.GetCollection<AzureCosmosDBMongoVCoreMemoryRecord>(collectionName).Indexes.ToList().contains(index)) {
            var command;
            switch (this._config.GetKind)
            {
                case "vector-ivf":
                    command = GetIndexDefinitionVectorIVF(index)
                    break;
                case "vector-hnsw":
                    command = GetIndexDefinitionVectorHNSW(index)
                    break;    
            }

            await _cosmosMongoDatabase.RunCommandAsync<BsonDocument>(command);
        }
        this._cosmosMongoCollection = this._cosmosMongoDatabase.GetCollection<AzureCosmosDBMongoVCoreMemoryRecord>(collectionName);
    }

    private BsonDocument GetIndexDefinitionVectorIVF(string index)
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
                                    { "kind", this._config.GetKind },
                                    { "numLists", this._config.GetNumLists },
                                    { "similarity", this._config.GetSimilarity },
                                    { "dimensions", this._config.GetDimensions}
                                }
                            }
                        }
                    }
                }
            };
    }

    private BsonDocument GetIndexDefinitionVectorHNSW(string index)
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
                                { "kind", this._config.GetKind },
                                { "m", this._config.GetNumberOfConnections },
                                { "efConstruction", this._config.GetEfConstruction},
                                { "similarity", this._config.GetSimilarity },
                                { "dimensions", this._config.GetDimensions}
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
        return this._cosmosMongoCollection.Indexes.ToList();
    }

    /// <inheritdoc />
    public async Task DeleteIndexAsync(string index, CancellationToken cancellationToken = default)
    {
        return this._cosmosMongoCollection.Indexes.DropIndexAsync(index);    
    }

    /// <inheritdoc />
    public async Task<string> UpsertAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        AzureCosmosDBMongoVCoreMemoryRecord localRecord = AzureCosmosDBMongoVCoreMemoryRecord.FromMemoryRecord(record);

        await _cosmosMongoCollection.InsertOneAsync(localRecord, new InsertOneOptions(), cancellationToken);

        return record.Id;
    }

    /// <inheritdoc />
    IAsyncEnumerable<(MemoryRecord, double)> GetSimilarListAsync(
        string index,
        string text,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        int limit = 1,
        bool withEmbeddings = false,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0) { limit=int.MaxValue;}

        Embedding testEmbedding = await this._embeddingGenerator.GenerateEmbeddingAsync(text, cancellationToken).ConfigureAwait(false);

        var pipeline;
            switch (this._config.GetKind)
            {
                case "vector-ivf":
                    pipeline = GetVectorIVFSearchPipeline(embedding, limit)
                    break;
                case "vector-hnsw":
                    pipeline = GetVectorHNSWSearchPipeline(embedding, limit)
                    break;    
            }

        await foreach (AzureCosmosDBMongoVCoreMemoryRecord doc in this._cosmosMongoCollection.AggregateAsync<BsonDocument>(pipeline))
        {
            if (doc == null || doc.similarityScore < minRelevance) { continue; }

            MemoryRecord memoryRecord = doc.Document.ToMemoryRecord(withEmbeddings);

            yield return (memoryRecord, doc.similarityScore);
        }
    }

    private BsonDocument GetVectorIVFSearchPipeline(string embedding, int limit)
    {
        return new List<BsonDocument>
        {
            new BsonDocument
            {
                { "$search", new BsonDocument
                    {
                        { "cosmosSearch", new BsonDocument
                            {
                                { "vector", embedding.ToList() },
                                { "path", "embedding" },
                                { "k", limit }
                            }
                        },
                        { "returnStoredSource", true }
                    }
                }
            },
            new BsonDocument
            {
                { "$project", new BsonDocument
                    {
                        { "similarityScore", new BsonDocument { { "$meta", "searchScore" } } },
                        { "document", "$$ROOT" }
                    }
                }
            }
        };  
    }

    private BsonDocument GetVectorHNSWSearchPipeline(string embedding, int limit)
    {
        return new List<BsonDocument>
        {
            new BsonDocument
            {
                { "$search", new BsonDocument
                    {
                        "cosmosSearch", new BsonDocument
                        {
                            { "vector", embedding.ToList() },
                            { "path", "embedding" },
                            { "k", limit },
                            { "efSearch", this._config.GetEfSearch}
                        }
                    }
                }
            }
        };
    }

    IAsyncEnumerable<MemoryRecord> GetListAsync(
        string index,
        ICollection<MemoryFilter>? filters = null,
        int limit = 1,
        bool withEmbeddings = false,
        CancellationToken cancellationToken = default)
    {
        # TODO: Add logic for search with filters, once it is released in MongoDB vCore.
        # For now this method returns an empty list.
        return AsyncEnumerable.Empty<MemoryRecord>();
    }    

    public async Task DeleteAsync(
        string index, 
        MemoryRecord record,
        CancellationToken cancellationToken = default)
    {
        AzureCosmosDBMongoVCoreMemoryRecord localRecord = AzureCosmosDBMongoVCoreMemoryRecord.FromMemoryRecord(record);
        var filter = Builders<AzureCosmosDBMongoVCoreMemoryRecord>.Filter.Eq(r => r.Id, localRecord.Id)
        return await _cosmosMongoCollection.DeleteOneAsync(filter, cancellationToken);
    }
}

