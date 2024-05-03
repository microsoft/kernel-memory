// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http;

namespace Microsoft.KernelMemory.MemoryDb.Qdrant.Client.Http;

internal sealed class DeleteCollectionRequest
{
    private readonly string _collectionName;

    public static DeleteCollectionRequest Create(string collectionName)
    {
        return new DeleteCollectionRequest(collectionName);
    }

    public HttpRequestMessage Build()
    {
        this.Validate();
        return HttpRequest.CreateDeleteRequest($"collections/{this._collectionName}?timeout=30");
    }

    private DeleteCollectionRequest(string collectionName)
    {
        this._collectionName = collectionName;
    }

    private void Validate()
    {
        ArgumentNullExceptionEx.ThrowIfNullOrWhiteSpace(this._collectionName, nameof(this._collectionName), "The collection name is empty");
    }
}
