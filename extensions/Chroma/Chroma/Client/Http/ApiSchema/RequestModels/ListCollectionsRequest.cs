// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http;

namespace Microsoft.KernelMemory.MemoryDb.Chroma.Client.Http.ApiSchema.RequestModels;

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

    #region private ================================================================================

    private ListCollectionsRequest()
    {
    }

    #endregion
}
