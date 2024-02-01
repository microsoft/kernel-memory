// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using KernelMemory.AtlasMongoDb.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryStorage;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Alkampfer.KernelMemory.AtlasMongoDb;

public class MongoDbVectorMemory : MongoDbKernelMemoryBaseStorage, IMemoryDb
{
    private readonly ITextEmbeddingGenerator _embeddingGenerator;
    private readonly ILogger<MongoDbVectorMemory> _log;
    private readonly AtlasSearchHelper _utils;
    private const string ConnectionNamePrefix = "_ix_";

    /// <summary>
    /// Create a new instance of MongoDbVectorMemory from configuration
    /// </summary>
    /// <param name="config">Configuration</param>
    /// <param name="embeddingGenerator">Embedding generator</param>
    /// <param name="log">Application logger</param>
    public MongoDbVectorMemory(
        MongoDbKernelMemoryConfiguration config,
        ITextEmbeddingGenerator embeddingGenerator,
        ILogger<MongoDbVectorMemory>? log = null) : base(config)
    {
        this._embeddingGenerator = embeddingGenerator ?? throw new ArgumentNullException(nameof(embeddingGenerator));
        this._log = log ?? DefaultLogger<MongoDbVectorMemory>.Instance;

        this._utils = new AtlasSearchHelper(this.Config.ConnectionString, this.Config.DatabaseName);
    }

    #region Basic helper functions

    private static string NormalizeIndexName(string indexName)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            return Constants.DefaultIndex;
        }

        return indexName.Replace("_", "-", StringComparison.OrdinalIgnoreCase);
    }

    private IMongoCollection<BsonDocument> GetCollectionFromIndexName(string indexName)
    {
        var collectionName = this.GetCollectionName(NormalizeIndexName(indexName));
        return this.GetCollection(collectionName);
    }

    private string GetCollectionName(string indexName)
    {
        var normalizedIndexName = NormalizeIndexName(indexName);
        if (this.Config.UseSingleCollectionForVectorSearch)
        {
            return $"{ConnectionNamePrefix}_kernel_memory_single_index";
        }

        return $"{ConnectionNamePrefix}{normalizedIndexName}";
    }

    #endregion

    public async Task CreateIndexAsync(string index, int vectorSize, CancellationToken cancellationToken = default)
    {
        var normalizedIndexName = NormalizeIndexName(index);
        //Index name is the name of the collection, so we need to understand if the collection exists
        var collectionName = this.GetCollectionName(index);
        await this._utils.CreateIndexAsync(collectionName, vectorSize).ConfigureAwait(false);
        await this._utils.WaitForIndexToBeReadyAsync(collectionName, 120).ConfigureAwait(false);

        //Keep tracks of created indexes.
        var collection = this.Database.GetCollection<BsonDocument>(GetIndexListCollectionName());
        //upsert the name of the index
        var filter = Builders<BsonDocument>.Filter.Eq("_id", normalizedIndexName);
        var update = Builders<BsonDocument>.Update.Set("index", normalizedIndexName).Set("lastCreateIndex", DateTime.UtcNow);
        await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
    }

    private static string GetIndexListCollectionName()
    {
        return $"{ConnectionNamePrefix}_kernel_memory_index_lists";
    }

    public async Task DeleteIndexAsync(string index, CancellationToken cancellationToken = default)
    {
        var normalizedIndexName = NormalizeIndexName(index);
        if (this.Config.UseSingleCollectionForVectorSearch)
        {
            //actually if we use a single collection we do not delete the entire collection we simply delete records of the index
            var collection = this.GetCollectionFromIndexName(index);
            await collection.DeleteManyAsync(x => x["index"] == normalizedIndexName, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var collectionName = this.GetCollectionName(index);
            await this._utils.DeleteIndicesAsync(collectionName).ConfigureAwait(false);
            await this.Database.DropCollectionAsync(collectionName, cancellationToken).ConfigureAwait(false);
        }

        var collectionIndexList = this.Database.GetCollection<BsonDocument>(GetIndexListCollectionName());
        await collectionIndexList.DeleteOneAsync(x => x["_id"] == index, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<string>> GetIndexesAsync(CancellationToken cancellationToken = default)
    {
        //we load index from the index list collection
        var collection = this.Database.GetCollection<BsonDocument>(GetIndexListCollectionName());
        var cursor = await collection.FindAsync(Builders<BsonDocument>.Filter.Empty, cancellationToken: cancellationToken).ConfigureAwait(false);
        return cursor.ToEnumerable(cancellationToken: cancellationToken).Select(x => x["_id"].AsString).ToImmutableArray();
    }

    public Task DeleteAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        var collection = this.GetCollectionFromIndexName(index);
        return collection.DeleteOneAsync(x => x["_id"] == record.Id, cancellationToken: cancellationToken);
    }

    public async IAsyncEnumerable<MemoryRecord> GetListAsync(
        string index,
        ICollection<MemoryFilter>? filters = null,
        int limit = 1,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            limit = 10;
        }

        //need to create a search query and execute it
        var normalizedIndexName = NormalizeIndexName(index);
        var conditions = new BsonArray();
        this.ConvertsFilterToConditions(normalizedIndexName, filters, conditions);
        BsonDocument[] pipeline = await this.CreatePipelineAsync(normalizedIndexName, conditions, limit, null).ConfigureAwait(false);

        var collection = this.GetCollectionFromIndexName(index);

        using var cursor = await collection.AggregateAsync<BsonDocument>(pipeline, cancellationToken: cancellationToken).ConfigureAwait(false);

        IEnumerable<BsonDocument> documents = cursor.ToEnumerable(cancellationToken: cancellationToken).Take(limit);
        foreach (var document in documents)
        {
            yield return FromBsonDocument(document, withEmbeddings);
        }
    }

    public async IAsyncEnumerable<(MemoryRecord, double)> GetSimilarListAsync(
        string index,
        string text,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        int limit = 1,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            limit = 10;
        }

        //need to create a search query and execute it
        var normalizedIndexName = NormalizeIndexName(index);
        var conditions = new BsonArray();
        this.ConvertsFilterToConditions(normalizedIndexName, filters, conditions);

        var embeddings = await this._embeddingGenerator.GenerateEmbeddingAsync(text, cancellationToken).ConfigureAwait(false);
        BsonDocument[] pipeline = await this.CreatePipelineAsync(normalizedIndexName, conditions, limit, embeddings).ConfigureAwait(false);

        // add the $meta to get the score
        var projectStage = new BsonDocument
        {
            {
                "$project", new BsonDocument
                {
                    { "document", "$$ROOT" },
                    { "score", new BsonDocument { { "$meta", "searchScore" } } }
                }
            }
        };

        pipeline = pipeline.Append(projectStage).ToArray();
        var collection = this.GetCollectionFromIndexName(normalizedIndexName);

        using var cursor = await collection.AggregateAsync<BsonDocument>(pipeline, cancellationToken: cancellationToken).ConfigureAwait(false);
        var documents = cursor.ToEnumerable(cancellationToken: cancellationToken).Take(limit);

        // If you check documentation Atlas normalize the score with formula
        // score = (1 + cosine/dot_product(v1,v2)) / 2
        // Thus it does not output the real cosine similarity, this is annoying so we
        // need to recompute cosine similarity using the embeddings.
#if DEBUG
        //List<(double, double, double)> similarities = new();
#endif
        foreach (var document in documents)
        {
            var memoryRecordDocument = (BsonDocument)document["document"];

            var memoryRecord = FromBsonDocument(memoryRecordDocument, withEmbeddings);
            var vector = memoryRecordDocument["embedding"].AsBsonArray.Select(x => x.AsDouble).ToArray();
            // we have score that is normalized, so we need to recompute similarity to have a real cosine distance
            var cosineSimilarity = CosineSim(embeddings, vector);
            if (cosineSimilarity < minRelevance)
            {
                //we have reached the limit for minimum relevance so we can stop iterating
                break;
            }
#if DEBUG
            // Tried to calculate cosine similarity thanks to helper function of embeddings class but the precision
            // is different from what is used by the tests, so test fails. This forces me to use actually the
            // CosineSim formula. This commented code can be use in debugging to verify the differences between
            // various values
            // var score = document["score"].AsDouble;
            // var cosineSimilarity2 = embeddings.CosineSimilarity(memoryRecord.Vector);
            // similarities.Add((score, cosineSimilarity, cosineSimilarity2));
            // var diff = Math.Abs(cosineSimilarity - cosineSimilarity2);
#endif
            yield return (memoryRecord, cosineSimilarity);
        }
    }

    public async Task<string> UpsertAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        var normalizedIndexName = NormalizeIndexName(index);
        var collection = this.GetCollectionFromIndexName(index);
        BsonDocument bsonDocument = new()
        {
            ["_id"] = record.Id,
            ["index"] = normalizedIndexName,
            ["embedding"] = new BsonArray(record.Vector.Data.Span.ToArray())
        };
        foreach (var (key, value) in record.Payload)
        {
            bsonDocument[$"pl_{key}"] = value?.ToString();
        }

        foreach (var (key, value) in record.Tags)
        {
            bsonDocument[$"tg_{key}"] = new BsonArray(value);
        }

        await collection.InsertOneAsync(bsonDocument, cancellationToken: cancellationToken).ConfigureAwait(false);
        await this.Config.AfterIndexCallbackAsync().ConfigureAwait(false);
        return bsonDocument["_id"].AsString;
    }

    #region Helper functions for query

    /// <summary>
    /// Create the real pipeline operator to send to the database to perform the search.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="filters"></param>
    /// <param name="limit"></param>
    /// <param name="embeddings">Used to perform a Knn query</param>
    /// <returns></returns>
    private async Task<BsonDocument[]> CreatePipelineAsync(
        string index,
        BsonArray filters,
        int limit,
        Embedding? embeddings)
    {
        //we have two distinct way of search based on textual query.
        if (embeddings == null)
        {
            //ok we have a simple filter query where we need to apply only filter
            var compound = new BsonDocument();
            compound["must"] = filters;
            return new BsonDocument[]
            {
                new BsonDocument
                {
                    {
                        "$search", new BsonDocument
                        {
                            { "index", this._utils.GetIndexName(this.GetCollectionName(index)) },
                            { "compound", compound }
                        }
                    }
                },
                new BsonDocument
                {
                    {
                        "$limit", limit
                    }
                }
            };
        }

        //now the query is performed using knnBeta.
        var aggregationSearchPipeline = new BsonDocument[]
        {
            new()
            {
                {
                    "$search", new BsonDocument
                    {
                        { "index", this._utils.GetIndexName(this.GetCollectionName(index)) },
                        {
                            "knnBeta", new BsonDocument
                            {
                                { "vector", new BsonArray(embeddings.Value.Data.ToArray()) },
                                { "path", "embedding" },
                                { "k", limit }
                            }
                        }
                    }
                }
            },
        };

        //If some filters are present we need to add them to the pipeline, if not filter is present do
        //not add anything because the query will be invalid.
        var compoundFilter = new BsonDocument();
        if (filters.Count > 0)
        {
            var knnFilters = new BsonDocument();
            knnFilters["must"] = filters;
            compoundFilter["compound"] = knnFilters;

            aggregationSearchPipeline[0]["$search"]["knnBeta"]["filter"] = compoundFilter;
        }

        return aggregationSearchPipeline;
    }

    private void ConvertsFilterToConditions(
        string index,
        ICollection<MemoryFilter>? filters,
        BsonArray conditions)
    {
        //this is a must filter, a condition that must be satisfied because we need to search into the index.
        if (this.Config.UseSingleCollectionForVectorSearch)
        {
            //need to add an index filter we have all data of index in the same collection
            var condition = new BsonDocument
            {
                ["term"] = new BsonDocument
                {
                    { "index", index }
                }
            };
        }

        //then semantic is that each element in filters must be an or. Each filter
        //can contains more than one condition, that is in and
        BsonArray filtersArray = new();
        foreach (var filter in filters ?? Array.Empty<MemoryFilter>())
        {
            var thisFilter = filter.GetFilters().ToArray();
            //this is an and filter, we can distinguish between single filter and multiple filters
            var numOfFilters = thisFilter.Count(x => !String.IsNullOrEmpty(x.Value));
            if (numOfFilters == 1)
            {
                var (key, value) = thisFilter.First(x => !String.IsNullOrEmpty(x.Value));
                filtersArray.Add(new BsonDocument
                {
                    ["text"] = new BsonDocument
                    {
                        { "query", value },
                        { "path", $"tg_{key}" }
                    }
                });
            }
            else if (numOfFilters > 1)
            {
                //we need to create an AND compound
                BsonArray andArray = new();
                foreach (var (key, value) in thisFilter.Where(f => !string.IsNullOrEmpty(f.Value)))
                {
                    var condition = new BsonDocument
                    {
                        ["text"] = new BsonDocument
                        {
                            { "query", value },
                            { "path", $"tg_{key}" }
                        }
                    };
                    andArray.Add(condition);
                }

                filtersArray.Add(new BsonDocument
                {
                    ["compound"] = new BsonDocument
                    {
                        ["must"] = andArray
                    }
                });
            }
        }

        //how many filter we have?
        if (filtersArray.Count == 1)
        {
            conditions.Add(filtersArray[0]);
        }
        else if (filtersArray.Count > 1)
        {
            //we need to create an OR compound
            var orCompound = new BsonDocument
            {
                ["compound"] = new BsonDocument
                {
                    ["should"] = filtersArray
                }
            };
            conditions.Add(orCompound);
        }
    }

    private static MemoryRecord FromBsonDocument(BsonDocument doc, Boolean withEmbeddings)
    {
        var record = new MemoryRecord
        {
            Id = doc["_id"].AsString,
            Vector = withEmbeddings ? doc["embedding"].AsBsonArray.Select(x => (float)x.AsDouble).ToArray() : Array.Empty<float>(),
        };
        foreach (var element in doc.Elements)
        {
            if (element.Name.StartsWith("pl_", StringComparison.OrdinalIgnoreCase))
            {
                var key = element.Name.Replace("pl_", "", StringComparison.OrdinalIgnoreCase);
                record.Payload[key] = element.Value.AsString;
            }
            else if (element.Name.StartsWith("tg_", StringComparison.OrdinalIgnoreCase))
            {
                var key = element.Name.Replace("tg_", "", StringComparison.OrdinalIgnoreCase);
                record.Tags[key] = element.Value.AsBsonArray.Select(x => (string?)x.AsString).ToList();
            }
        }

        return record;
    }

    #endregion

    /// <summary>
    /// Due to different score system of Atlas MongoDB that normalized cosine
    /// we need to manually recompute the cosine similarity distance manually
    /// for each vector to have a real cosine similarity distance returned.
    /// </summary>
    /// <param name="vec1"></param>
    /// <param name="vec2"></param>
    /// <returns></returns>
    private static double CosineSim(Embedding vec1, double[] vec2)
    {
        var v1 = vec1.Data.ToArray();
        var v2 = vec2;

        int size = vec1.Length;
        double dot = 0.0d;
        double m1 = 0.0d;
        double m2 = 0.0d;
        for (int n = 0; n < size; n++)
        {
            dot += v1[n] * v2[n];
            m1 += Math.Pow(v1[n], 2);
            m2 += Math.Pow(v2[n], 2);
        }

        double cosineSimilarity = dot / (Math.Sqrt(m1) * Math.Sqrt(m2));
        return cosineSimilarity;
    }
}
