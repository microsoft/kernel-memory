// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.MemoryDb.Chroma.Client.Http.ApiSchema.RequestModels;

internal sealed class GetCollectionRequest
{
    [JsonIgnore]
    public string CollectionName { get; set; }

    public static GetCollectionRequest Create(string collectionName)
    {
        return new GetCollectionRequest(collectionName);
    }

    public HttpRequestMessage Build()
    {
        return HttpRequest.CreateGetRequest($"collections/{this.CollectionName}");
    }

    #region private ================================================================================

    private GetCollectionRequest(string collectionName)
    {
        this.CollectionName = collectionName;
    }

    #endregion
}
