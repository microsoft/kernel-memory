using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.MongoDbAtlas.Helpers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Microsoft.KernelMemory.MongoDbAtlas;

public class MongoDbVectorMemory : MongoDbKernelMemoryBaseStorage, IMemoryDb
{
    private const string ConnectionNamePrefix = "_ix_";

    private readonly ITextEmbeddingGenerator _embeddingGenerator;
    private readonly ILogger<MongoDbVectorMemory> _log;
    private readonly AtlasSearchHelper _utils;

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

    public async Task DeleteIndexAsync(string index, CancellationToken cancellationToken = default)
    {
        var normalizedIndexName = NormalizeIndexName(index);
        if (this.Config.UseSingleCollectionForVectorSearch)
        {
            //actually if we use a single collection we do not delete the entire collection we simply delete records of the index
            var collection = this.GetCollectionFromIndexName(index);
            await collection.DeleteManyAsync(x => x.Index == normalizedIndexName, cancellationToken: cancellationToken).ConfigureAwait(false);
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
        return collection.DeleteOneAsync(x => x.Id == record.Id, cancellationToken: cancellationToken);
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

        //need to create a query and execute it without using $vector
        var collection = this.GetCollectionFromIndexName(index);
        var finalFilter = this.TranslateFilters(filters, index);

        // we need to performa a simple query without using vector search
        var cursor = await collection.FindAsync(finalFilter, cancellationToken: cancellationToken).ConfigureAwait(false);
        var documents = await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var document in documents)
        {
            var memoryRecord = FromMongodbMemoryRecord(document, withEmbeddings);

            yield return memoryRecord;
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
        var collectionName = this.GetCollectionName(index);
        var embeddings = await this._embeddingGenerator.GenerateEmbeddingAsync(text, cancellationToken).ConfigureAwait(false);

        // define vector embeddings to search
        var vector = embeddings.Data.Span.ToArray();

        //need to create the filters
        var finalFilter = this.TranslateFilters(filters, index);

        var options = new VectorSearchOptions<MongoDbAtlasMemoryRecord>()
        {
            IndexName = this._utils.GetIndexName(collectionName),
            NumberOfCandidates = limit,
            Filter = finalFilter
        };
        var collection = this.GetCollectionFromIndexName(index);

        // run query
        var documents = await collection.Aggregate()
            .VectorSearch(m => m.Embedding, vector, limit, options)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        // If you check documentation Atlas normalize the score with formula
        // score = (1 + cosine/dot_product(v1,v2)) / 2
        // Thus it does not output the real cosine similarity, this is annoying so we
        // need to recompute cosine similarity using the embeddings.
#if DEBUG
        //List<(double, double, double)> similarities = new();
#endif
        foreach (var document in documents)
        {
            var memoryRecord = FromMongodbMemoryRecord(document, withEmbeddings);

            // we have score that is normalized, so we need to recompute similarity to have a real cosine distance
            var cosineSimilarity = CosineSim(embeddings, document.Embedding);
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

    /// <inheritdoc />
    public async Task<string> UpsertAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        var normalizedIndexName = NormalizeIndexName(index);
        var collection = this.GetCollectionFromIndexName(index);
        MongoDbAtlasMemoryRecord mongoRecord = new()
        {
            Id = record.Id,
            Index = normalizedIndexName,
            Embedding = record.Vector.Data.ToArray(),
            Tags = record.Tags.Select(x => new MongoDbAtlasMemoryRecord.Tag(x.Key, x.Value.ToArray())).ToList(),
            Payloads = record.Payload.Select(x => new MongoDbAtlasMemoryRecord.Payload(x.Key, x.Value)).ToList()
        };

        await collection.InsertOneAsync(mongoRecord, cancellationToken: cancellationToken).ConfigureAwait(false);
        await this.Config.AfterIndexCallbackAsync().ConfigureAwait(false);
        return record.Id;
    }

    internal class MongoDbAtlasMemoryRecord
    {
        public string Id { get; set; }
        public string Index { get; set; }
        public float[] Embedding { get; set; }
        public List<Tag> Tags { get; set; } = new();
        public List<Payload> Payloads { get; set; } = new();

        internal class Payload
        {
            public Payload(string key, object value)
            {
                this.Key = key;
                this.Value = value;
            }

            public string Key { get; set; }
            public object Value { get; set; }
        }

        internal class Tag
        {
            public Tag(string key, string?[] values)
            {
                this.Key = key;
                this.Values = values;
            }

            public string Key { get; set; }
            public string?[] Values { get; set; }
        }
    }

    private FilterDefinition<MongoDbAtlasMemoryRecord>? TranslateFilters(ICollection<MemoryFilter>? filters, string index)
    {
        List<FilterDefinition<MongoDbAtlasMemoryRecord>> outerFiltersArray = new();
        foreach (var filter in filters ?? Array.Empty<MemoryFilter>())
        {
            var thisFilter = filter.GetFilters().ToArray();
            var numOfFilters = thisFilter.Count(x => !string.IsNullOrEmpty(x.Value));
            List<FilterDefinition<MongoDbAtlasMemoryRecord>> filtersArray = new();
            foreach (var singleFilter in thisFilter)
            {
                var condition = Builders<MongoDbAtlasMemoryRecord>.Filter.And(
                    Builders<MongoDbAtlasMemoryRecord>.Filter.Eq("Tags.Key", singleFilter.Key),
                    Builders<MongoDbAtlasMemoryRecord>.Filter.Eq("Tags.Values", singleFilter.Value)
                );
                filtersArray.Add(condition);
            }

            //all these filter if more than one must be enclosed in an end filter
            if (filtersArray.Count > 1)
            {
                //More than one condition we need to create the condition
                var andFilter = Builders<MongoDbAtlasMemoryRecord>.Filter.And(filtersArray);
                outerFiltersArray.Add(andFilter);
            }
            else if (filtersArray.Count == 1)
            {
                //we do not need to include an and filter because we have only one condition 
                outerFiltersArray.Add(filtersArray[0]);
            }
        }

        FilterDefinition<MongoDbAtlasMemoryRecord>? finalFilter = null;

        //outer filters must be composed in or
        if (outerFiltersArray.Count > 1)
        {
            finalFilter = Builders<MongoDbAtlasMemoryRecord>.Filter.Or(outerFiltersArray);
        }
        else if (outerFiltersArray.Count == 1)
        {
            //we do not need to include an or filter because we have only one condition
            finalFilter = outerFiltersArray[0];
        }

        //remember that if we are usins a single collection for all records we need to add an index filter
        if (this.Config.UseSingleCollectionForVectorSearch)
        {
            var indexFilter = Builders<MongoDbAtlasMemoryRecord>.Filter.Eq("Index", index);
            if (finalFilter == null)
            {
                finalFilter = indexFilter;
            }
            else
            {
                //compose in and
                finalFilter = Builders<MongoDbAtlasMemoryRecord>.Filter.And(indexFilter, finalFilter);
            }
        }

        return finalFilter;
    }

    private IMongoCollection<MongoDbAtlasMemoryRecord> GetCollectionFromIndexName(string indexName)
    {
        var collectionName = this.GetCollectionName(NormalizeIndexName(indexName));
        return this.GetCollection<MongoDbAtlasMemoryRecord>(collectionName);
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

    /// <summary>
    /// Create the real pipeline operator to send to the database to perform the search.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="filters"></param>
    /// <param name="limit"></param>
    /// <param name="embeddings">Used to perform a Knn query</param>
    /// <returns></returns>
    private BsonDocument[] CreatePipeline(
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
                new() {
                    {
                        "$search", new BsonDocument
                        {
                            { "index", this._utils.GetIndexName(this.GetCollectionName(index)) },
                            { "compound", compound }
                        }
                    }
                },
                new() {
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
            var numOfFilters = thisFilter.Count(x => !string.IsNullOrEmpty(x.Value));
            if (numOfFilters == 1)
            {
                var (key, value) = thisFilter.First(x => !string.IsNullOrEmpty(x.Value));
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

    /// <summary>
    /// Due to different score system of Atlas MongoDB that normalized cosine
    /// we need to manually recompute the cosine similarity distance manually
    /// for each vector to have a real cosine similarity distance returned.
    /// </summary>
    /// <param name="vec1"></param>
    /// <param name="vec2"></param>
    /// <returns></returns>
    private static double CosineSim(Embedding vec1, float[] vec2)
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

    private static string GetIndexListCollectionName()
    {
        return $"{ConnectionNamePrefix}_kernel_memory_index_lists";
    }

    private static MemoryRecord FromMongodbMemoryRecord(MongoDbAtlasMemoryRecord doc, bool withEmbeddings)
    {
        var record = new MemoryRecord
        {
            Id = doc.Id,
            Vector = withEmbeddings ? doc.Embedding : Array.Empty<float>(),
        };

        foreach (var tag in doc.Tags)
        {
            record.Tags[tag.Key] = tag.Values.ToList();
        }

        foreach (var payload in doc.Payloads)
        {
            record.Payload[payload.Key] = payload.Value;
        }

        return record;
    }

    private static string NormalizeIndexName(string indexName)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            return Constants.DefaultIndex;
        }

        return indexName.Replace("_", "-", StringComparison.OrdinalIgnoreCase);
    }
}
