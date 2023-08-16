// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http;

namespace Microsoft.SemanticMemory.Core.MemoryStorage.Qdrant.Client.Http;

internal sealed class ListCollectionsRequest
{
    public static ListCollectionsRequest Create()
    {
        return new ListCollectionsRequest();
    }

    public HttpRequestMessage Build()
    {
        return HttpRequest.CreateGetRequest("collections");
    }
}
