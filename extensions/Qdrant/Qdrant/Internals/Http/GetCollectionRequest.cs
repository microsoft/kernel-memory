// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http;

namespace Microsoft.KernelMemory.MemoryDb.Qdrant.Client.Http;

internal sealed class GetCollectionsRequest
{
    private readonly string _collectionName;

    public static GetCollectionsRequest Create(string collectionName)
    {
        return new GetCollectionsRequest(collectionName);
    }

    public HttpRequestMessage Build()
    {
        return HttpRequest.CreateGetRequest($"collections/{this._collectionName}");
    }

    private GetCollectionsRequest(string collectionName)
    {
        this._collectionName = collectionName;
    }
}
