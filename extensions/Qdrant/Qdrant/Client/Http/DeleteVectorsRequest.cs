// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.MemoryDb.Qdrant.Client.Http;

internal sealed class DeleteVectorsRequest
{
    private readonly string _collectionName;

    [JsonPropertyName("points")]
    public List<Guid> Ids { get; set; }

    public static DeleteVectorsRequest DeleteFrom(string collectionName)
    {
        return new DeleteVectorsRequest(collectionName);
    }

    public DeleteVectorsRequest DeleteVector(Guid qdrantPointId)
    {
        Verify.NotNull(qdrantPointId, "The point ID is NULL");
        this.Ids.Add(qdrantPointId);
        return this;
    }

    public DeleteVectorsRequest DeleteRange(IEnumerable<Guid> qdrantPointIds)
    {
        Verify.NotNull(qdrantPointIds, "The point ID collection is NULL");
        this.Ids.AddRange(qdrantPointIds);
        return this;
    }

    public HttpRequestMessage Build()
    {
        this.Validate();
        return HttpRequest.CreatePostRequest(
            $"collections/{this._collectionName}/points/delete",
            payload: this);
    }

    private DeleteVectorsRequest(string collectionName)
    {
        this.Ids = new List<Guid>();
        this._collectionName = collectionName;
    }

    private void Validate()
    {
        Verify.NotNullOrEmpty(this._collectionName, "The collection name is empty");
        Verify.NotNullOrEmpty(this.Ids, "The list of vectors to delete is NULL or empty");
    }
}
