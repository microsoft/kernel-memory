// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.MemoryDb.Qdrant.Client.Http;

internal sealed class SearchVectorsRequest
{
    private readonly string _collectionName;

    [JsonPropertyName("vector")]
    [JsonConverter(typeof(Embedding.JsonConverter))]
    public Embedding StartingVector { get; set; }

    [JsonPropertyName("filter")]
    public Filter.AndClause Filters { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("with_payload")]
    public bool WithPayload { get; set; }

    [JsonPropertyName("with_vector")]
    public bool WithVector { get; set; }

    [JsonPropertyName("score_threshold")]
    public double ScoreThreshold { get; set; } = -1;

    public static SearchVectorsRequest Create(string collectionName)
    {
        return new SearchVectorsRequest(collectionName);
    }

    public static SearchVectorsRequest Create(string collectionName, int vectorSize)
    {
        return new SearchVectorsRequest(collectionName).SimilarTo(new Embedding(vectorSize));
    }

    public SearchVectorsRequest SimilarTo(Embedding vector)
    {
        this.StartingVector = vector;
        return this;
    }

    public SearchVectorsRequest HavingExternalId(string id)
    {
        Verify.NotNull(id, "External ID is NULL");
        this.Filters.AndValue(QdrantConstants.PayloadIdField, id);
        return this;
    }

    public SearchVectorsRequest HavingAllTags(IEnumerable<string>? tags)
    {
        if (tags == null) { return this; }

        foreach (var tag in tags)
        {
            if (!string.IsNullOrEmpty(tag))
            {
                this.Filters.AndValue(QdrantConstants.PayloadTagsField, tag);
            }
        }

        return this;
    }

    public SearchVectorsRequest HavingSomeTags(IEnumerable<IEnumerable<string>?>? tagGroups)
    {
        if (tagGroups == null) { return this; }

        var list = tagGroups.ToList();
        if (list.Count < 2)
        {
            return this.HavingAllTags(list.FirstOrDefault());
        }

        var orFilter = new Filter.OrClause();
        foreach (var tags in list)
        {
            if (tags == null) { continue; }

            var andFilter = new Filter.AndClause();
            foreach (var tag in tags)
            {
                if (!string.IsNullOrEmpty(tag))
                {
                    andFilter.AndValue(QdrantConstants.PayloadTagsField, tag);
                }
            }

            orFilter.Or(andFilter);
        }

        this.Filters.And(orFilter);

        return this;
    }

    public SearchVectorsRequest WithScoreThreshold(double scoreThreshold)
    {
        this.ScoreThreshold = scoreThreshold;
        return this;
    }

    public SearchVectorsRequest IncludePayLoad()
    {
        this.WithPayload = true;
        return this;
    }

    public SearchVectorsRequest IncludeVectorData(bool withVector)
    {
        this.WithVector = withVector;
        return this;
    }

    public SearchVectorsRequest FromPosition(int offset)
    {
        this.Offset = offset;
        return this;
    }

    public SearchVectorsRequest Take(int count)
    {
        this.Limit = count;
        return this;
    }

    public SearchVectorsRequest TakeFirst()
    {
        return this.FromPosition(0).Take(1);
    }

    public HttpRequestMessage Build()
    {
        this.Validate();
        return HttpRequest.CreatePostRequest(
            $"collections/{this._collectionName}/points/search",
            payload: this);
    }

    private void Validate()
    {
        Verify.NotNull(this.StartingVector, "Missing target, either provide a vector or a vector size");
        Verify.NotNullOrEmpty(this._collectionName, "The collection name is empty");
        Verify.That(this.Limit > 0, "The number of vectors must be greater than zero");
        this.Filters.Validate();
    }

    private SearchVectorsRequest(string collectionName)
    {
        this._collectionName = collectionName;
        this.Filters = new Filter.AndClause();
        this.WithPayload = false;
        this.WithVector = false;

        // By default take the closest vector only
        this.FromPosition(0).TakeFirst();
    }
}
