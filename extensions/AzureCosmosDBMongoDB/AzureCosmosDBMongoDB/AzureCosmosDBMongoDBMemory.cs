// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryStorage;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Microsoft.KernelMemory.MemoryDb.AzureCosmosDBMongoDB;

/// <summary>
/// Azure Cosmos DB for MongoDB connector for Kernel Memory,
/// Read more about Azure Cosmos DB for MongoDB and vector search here:
/// https://learn.microsoft.com/en-us/azure/cosmos-db/mongodb/vcore/
/// https://learn.microsoft.com/en-us/azure/cosmos-db/mongodb/vcore/vector-search
/// </summary>
public class AzureCosmosDBMongoDBMemory : IMemoryDb
{
    private readonly ITextEmbeddingGenerator _embeddingGenerator;
    private readonly ILogger<AzureCosmosDBMongoDBMemory> _log;
    private readonly AzureCosmosDBMongoDBConfig _config;
    private readonly MongoClient _cosmosDBMongoClient;
    private readonly IMongoDatabase _cosmosMongoDatabase;
    private readonly IMongoCollection<AzureCosmosDBMongoDBMemoryRecord> _cosmosMongoCollection;

    /// <summary>
    /// Create a new instance
    /// </summary>
    /// <param name="config">Azure Cosmos DB for MongoDB configuration</param>
    /// <param name="embeddingGenerator">Text embedding generator</param>
    /// <param name="log">Application logger</param>
    public AzureCosmosDBMongoDBMemory(
        AzureCosmosDBMongoDBConfig config,
        ITextEmbeddingGenerator embeddingGenerator,
        ILogger<AzureCosmosDBMongoDBMemory>? log = null)
    {
        this._embeddingGenerator = embeddingGenerator;
        this._log = log ?? DefaultLogger<AzureCosmosDBMongoDBMemory>.Instance;
        this._config = config;

        if (string.IsNullOrEmpty(config.ConnectionString))
        {
            this._log.LogCritical("Azure Cosmos DB for MongoDB connection string is empty.");
            throw new AzureCosmosDBMongoDBMemoryException("Azure Cosmos DB for MongoDB connection string is empty.");
        }

        if (this._embeddingGenerator == null)
        {
            throw new AzureCosmosDBMongoDBMemoryException("Embedding generator not configured");
        }

        MongoClientSettings settings = MongoClientSettings.FromConnectionString(config.ConnectionString);
        settings.ApplicationName = config.ApplicationName;
        this._cosmosDBMongoClient = new MongoClient(settings);
        this._cosmosMongoDatabase = this._cosmosDBMongoClient.GetDatabase(config.DatabaseName);

        this._cosmosMongoDatabase.CreateCollectionAsync(config.CollectionName);
        this._cosmosMongoCollection = this._cosmosMongoDatabase.GetCollection<AzureCosmosDBMongoDBMemoryRecord>(config.CollectionName);
    }

    /// <inheritdoc />
    public async Task CreateIndexAsync(string index, int vectorSize, CancellationToken cancellationToken = default)
    {
        var indexes = await this._cosmosMongoCollection.Indexes.ListAsync(cancellationToken).ConfigureAwait(false);
        if (!indexes.ToList(cancellationToken).Any(index => index["name"] == index))
        {
            var command = new BsonDocument();
            switch (this._config.Kind)
            {
                case AzureCosmosDBVectorSearchType.VectorIVF:
                    command = this.GetIndexDefinitionVectorIVF(index, vectorSize);
                    break;
                case AzureCosmosDBVectorSearchType.VectorHNSW:
                    command = this.GetIndexDefinitionVectorHNSW(index, vectorSize);
                    break;
            }

            await this._cosmosMongoDatabase.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
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
        var indexesCursor = await this._cosmosMongoCollection.Indexes.ListAsync(cancellationToken).ConfigureAwait(false);
        var indexes = await indexesCursor.ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var indexDoc in indexes)
        {
            if (indexDoc["name"].AsString == index)
            {
                await this._cosmosMongoCollection.Indexes.DropOneAsync(index, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public async Task<string> UpsertAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        AzureCosmosDBMongoDBMemoryRecord localRecord = AzureCosmosDBMongoDBMemoryRecord.FromMemoryRecord(record);
        var filter = Builders<AzureCosmosDBMongoDBMemoryRecord>.Filter.Eq(r => r.Id, localRecord.Id);

        var replaceOptions = new ReplaceOptions() { IsUpsert = true };

        await this._cosmosMongoCollection.ReplaceOneAsync(filter, localRecord, replaceOptions, cancellationToken).ConfigureAwait(false);

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
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (limit <= 0) { limit = int.MaxValue; }

        Embedding embedding = await this._embeddingGenerator.GenerateEmbeddingAsync(text, cancellationToken).ConfigureAwait(false);

        BsonDocument[]? pipeline = null;
        switch (this._config.Kind)
        {
            case AzureCosmosDBVectorSearchType.VectorIVF:
                pipeline = this.GetVectorIVFSearchPipeline(embedding, limit);
                break;
            case AzureCosmosDBVectorSearchType.VectorHNSW:
                pipeline = this.GetVectorHNSWSearchPipeline(embedding, limit);
                break;
        }

        using var cursor = await this._cosmosMongoCollection.AggregateAsync<BsonDocument>(pipeline, cancellationToken: cancellationToken).ConfigureAwait(false);
        while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var doc in cursor.Current)
            {
                // Access the similarityScore from the BSON document
                var similarityScore = doc.GetValue("similarityScore").AsDouble;
                if (similarityScore < minRelevance) { continue; }

                MemoryRecord memoryRecord = AzureCosmosDBMongoDBMemoryRecord.ToMemoryRecord(doc, withEmbeddings);

                yield return (memoryRecord, similarityScore);
            }
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<MemoryRecord> GetListAsync(
        string index,
        ICollection<MemoryFilter>? filters = null,
        int limit = 1,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var finalFilter = this.TranslateFilters(filters, index);

        // We need to perform a simple query without using vector search
        var cursor = await this._cosmosMongoCollection
            .FindAsync(finalFilter,
                new FindOptions<AzureCosmosDBMongoDBMemoryRecord>()
                {
                    Limit = limit
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);
        var documents = await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var document in documents)
        {
            var memoryRecord = FromAzureCosmosMemoryRecord(document, withEmbeddings);
            yield return memoryRecord;
        }
    }

    private static MemoryRecord FromAzureCosmosMemoryRecord(AzureCosmosDBMongoDBMemoryRecord doc, bool withEmbeddings)
    {
        var record = new MemoryRecord
        {
            Id = doc.Id,
            Payload = BsonSerializer.Deserialize<Dictionary<string, object>>(doc.Payload)
                      ?? new Dictionary<string, object>(),
            Vector = withEmbeddings ? doc.Embedding ?? Array.Empty<float>() : Array.Empty<float>()
        };

        foreach (string[] keyValue in doc.Tags.Select(tag => tag.Split(Constants.ReservedEqualsChar, 2)))
        {
            string key = keyValue[0];
            string? value = keyValue.Length == 1 ? null : keyValue[1];
            record.Tags.Add(key, value);
        }

        return record;
    }

    /// <inheritedoc />
    public async Task DeleteAsync(
        string index,
        MemoryRecord record,
        CancellationToken cancellationToken = default)
    {
        AzureCosmosDBMongoDBMemoryRecord localRecord = AzureCosmosDBMongoDBMemoryRecord.FromMemoryRecord(record);
        var filter = Builders<AzureCosmosDBMongoDBMemoryRecord>.Filter.Eq(r => r.Id, localRecord.Id);
        await this._cosmosMongoCollection.DeleteOneAsync(filter, cancellationToken).ConfigureAwait(false);
    }

    private FilterDefinition<AzureCosmosDBMongoDBMemoryRecord>? TranslateFilters(ICollection<MemoryFilter>? filters, string index)
    {
        List<FilterDefinition<AzureCosmosDBMongoDBMemoryRecord>> outerFiltersArray = new();
        foreach (var filter in filters ?? Array.Empty<MemoryFilter>())
        {
            var thisFilter = filter.GetFilters().ToArray();
            List<FilterDefinition<AzureCosmosDBMongoDBMemoryRecord>> filtersArray = new();
            foreach (var singleFilter in thisFilter)
            {
                var condition = Builders<AzureCosmosDBMongoDBMemoryRecord>.Filter.And(
                    Builders<AzureCosmosDBMongoDBMemoryRecord>.Filter.Eq("Tags.Key", singleFilter.Key),
                    Builders<AzureCosmosDBMongoDBMemoryRecord>.Filter.Eq("Tags.Values", singleFilter.Value)
                );
                filtersArray.Add(condition);
            }

            // if we have more than one condition, we need to compose all conditions with AND
            // but if we have only a single filter we can directly use the filter.
            if (filtersArray.Count > 1)
            {
                // More than one condition we need to create the condition
                var andFilter = Builders<AzureCosmosDBMongoDBMemoryRecord>.Filter.And(filtersArray);
                outerFiltersArray.Add(andFilter);
            }
            else if (filtersArray.Count == 1)
            {
                // We do not need to include an and filter because we have only one condition
                outerFiltersArray.Add(filtersArray[0]);
            }
        }

        FilterDefinition<AzureCosmosDBMongoDBMemoryRecord>? finalFilter = null;
        // Outer filters must be composed in or
        if (outerFiltersArray.Count > 1)
        {
            finalFilter = Builders<AzureCosmosDBMongoDBMemoryRecord>.Filter.Or(outerFiltersArray);
        }
        else if (outerFiltersArray.Count == 1)
        {
            // We do not need to include an or filter because we have only one condition
            finalFilter = outerFiltersArray[0];
        }

        var indexFilter = Builders<AzureCosmosDBMongoDBMemoryRecord>.Filter.Eq("Index", index);
        if (finalFilter == null)
        {
            finalFilter = indexFilter;
        }
        else
        {
            // Compose in and
            finalFilter = Builders<AzureCosmosDBMongoDBMemoryRecord>.Filter.And(indexFilter, finalFilter);
        }
        return finalFilter;
    }

    private BsonDocument GetIndexDefinitionVectorIVF(string index, int vectorSize)
    {
        return new BsonDocument
        {
            { "createIndexes", this._config.CollectionName },
            {
                "indexes",
                new BsonArray
                {
                    new BsonDocument
                    {
                        { "name", index },
                        { "key", new BsonDocument { { "embedding", "cosmosSearch" } } },
                        {
                            "cosmosSearchOptions", new BsonDocument
                            {
                                { "kind", this._config.Kind.GetCustomName() },
                                { "numLists", this._config.NumLists },
                                { "similarity", this._config.Similarity.GetCustomName() },
                                { "dimensions", vectorSize }
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
            { "createIndexes", this._config.CollectionName },
            {
                "indexes",
                new BsonArray
                {
                    new BsonDocument
                    {
                        { "name", index },
                        { "key", new BsonDocument { { "embedding", "cosmosSearch" } } },
                        {
                            "cosmosSearchOptions", new BsonDocument
                            {
                                { "kind", this._config.Kind.GetCustomName() },
                                { "m", this._config.NumberOfConnections },
                                { "efConstruction", this._config.EfConstruction },
                                { "similarity", this._config.Similarity.GetCustomName() },
                                { "dimensions", vectorSize }
                            }
                        }
                    }
                }
            }
        };
    }

    private BsonDocument[] GetVectorIVFSearchPipeline(Embedding embedding, int limit)
    {
#pragma warning disable CA1305 // Specify IFormatProvider
        string searchStage = @"
        {
            ""$search"": {
                ""cosmosSearch"": {
                    ""vector"": [" + string.Join(",", embedding.Data.ToArray().Select(f => f.ToString())) + @"],
                    ""path"": ""embedding"",
                    ""k"": " + limit + @"
                },
                ""returnStoredSource"": true
            }
        }";
#pragma warning restore CA1305 // Specify IFormatProvider

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
#pragma warning disable CA1305 // Specify IFormatProvider
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
#pragma warning restore CA1305 // Specify IFormatProvider

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
}
