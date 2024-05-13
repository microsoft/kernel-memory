// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.MemoryDb.Qdrant.Client.Http;

internal sealed class ScrollVectorsRequest
{
    private readonly string _collectionName;

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

    public static ScrollVectorsRequest Create(string collectionName)
    {
        return new ScrollVectorsRequest(collectionName);
    }

    public ScrollVectorsRequest HavingExternalId(string id)
    {
        ArgumentNullExceptionEx.ThrowIfNullOrWhiteSpace(id, nameof(id), "External ID is empty");
        this.Filters.AndValue(QdrantConstants.PayloadIdField, id);
        return this;
    }

    public ScrollVectorsRequest HavingAllTags(IEnumerable<string>? tags)
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

    public ScrollVectorsRequest HavingSomeTags(IEnumerable<IEnumerable<string>?>? tagGroups)
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

    public ScrollVectorsRequest IncludePayLoad()
    {
        this.WithPayload = true;
        return this;
    }

    public ScrollVectorsRequest IncludeVectorData(bool withVector)
    {
        this.WithVector = withVector;
        return this;
    }

    public ScrollVectorsRequest FromPosition(int offset)
    {
        this.Offset = offset;
        return this;
    }

    public ScrollVectorsRequest Take(int count)
    {
        this.Limit = count;
        return this;
    }

    public ScrollVectorsRequest TakeFirst()
    {
        return this.FromPosition(0).Take(1);
    }

    public HttpRequestMessage Build()
    {
        this.Validate();
        return HttpRequest.CreatePostRequest(
            $"collections/{this._collectionName}/points/scroll",
            payload: this);
    }

    private void Validate()
    {
        ArgumentNullExceptionEx.ThrowIfNullOrWhiteSpace(this._collectionName, nameof(this._collectionName), "The collection name cannot be empty");
        ArgumentOutOfRangeExceptionEx.ThrowIfZeroOrNegative(this.Limit, nameof(this.Limit), "The max number of vectors to retrieve must be greater than zero");

        this.Filters.Validate();
    }

    private ScrollVectorsRequest(string collectionName)
    {
        this._collectionName = collectionName;
        this.Filters = new Filter.AndClause();
        this.WithPayload = false;
        this.WithVector = false;

        // By default take the closest vector only
        this.FromPosition(0).TakeFirst();
    }
}
