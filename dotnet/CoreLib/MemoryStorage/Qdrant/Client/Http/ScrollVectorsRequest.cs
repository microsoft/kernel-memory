// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace Microsoft.SemanticMemory.Core.MemoryStorage.Qdrant.Client.Http;

internal sealed class ScrollVectorsRequest
{
    private readonly string _collectionName;

    [JsonPropertyName("filter")]
    public Filter Filters { get; set; }

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
        Verify.NotNull(id, "External ID is NULL");
        this.Filters.ValueMustMatch(QdrantConstants.PayloadIdField, id);
        return this;
    }

    public ScrollVectorsRequest HavingTags(IEnumerable<string>? tags)
    {
        if (tags == null) { return this; }

        foreach (var tag in tags)
        {
            if (!string.IsNullOrEmpty(tag))
            {
                this.Filters.ValueMustMatch(QdrantConstants.PayloadTagsField, tag);
            }
        }

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
        Verify.NotNullOrEmpty(this._collectionName, "The collection name is empty");
        Verify.That(this.Limit > 0, "The number of vectors must be greater than zero");
        this.Filters.Validate();
    }

    private ScrollVectorsRequest(string collectionName)
    {
        this._collectionName = collectionName;
        this.Filters = new Filter();
        this.WithPayload = false;
        this.WithVector = false;

        // By default take the closest vector only
        this.FromPosition(0).TakeFirst();
    }
}
