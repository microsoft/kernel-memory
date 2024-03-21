// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.MemoryDb.Chroma.Client.Http.ApiSchema.RequestModels;

internal sealed class DeleteCollectionRequest
{
    [JsonIgnore]
    public string CollectionName { get; set; }

    public static DeleteCollectionRequest Create(string collectionName)
    {
        return new DeleteCollectionRequest(collectionName);
    }

    public HttpRequestMessage Build()
    {
        return HttpRequest.CreateDeleteRequest($"collections/{this.CollectionName}");
    }

    #region private ================================================================================

    private DeleteCollectionRequest(string collectionName)
    {
        this.CollectionName = collectionName;
    }

    #endregion
}
