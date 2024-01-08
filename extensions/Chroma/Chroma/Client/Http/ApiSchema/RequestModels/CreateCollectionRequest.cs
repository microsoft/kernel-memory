// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.MemoryDb.Chroma.Client.Http.ApiSchema.RequestModels;

internal sealed class CreateCollectionRequest
{
    [JsonPropertyName("name")]
    public string CollectionName { get; set; }

    [JsonPropertyName("get_or_create")]
    public bool GetOrCreate => true;

    public static CreateCollectionRequest Create(string collectionName)
    {
        return new CreateCollectionRequest(collectionName);
    }

    public HttpRequestMessage Build()
    {
        return HttpRequest.CreatePostRequest("collections", this);
    }

    #region private ================================================================================

    private CreateCollectionRequest(string collectionName)
    {
        this.CollectionName = collectionName;
    }

    #endregion
}
