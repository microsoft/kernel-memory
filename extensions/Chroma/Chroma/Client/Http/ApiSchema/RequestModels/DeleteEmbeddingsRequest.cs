// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.MemoryDb.Chroma.Client.Http.ApiSchema.RequestModels;

internal sealed class DeleteEmbeddingsRequest
{
    [JsonIgnore]
    public string CollectionId { get; set; }

    [JsonPropertyName("ids")]
    public string[] Ids { get; set; }

    public static DeleteEmbeddingsRequest Create(string collectionId, string[] ids)
    {
        return new DeleteEmbeddingsRequest(collectionId, ids);
    }

    public HttpRequestMessage Build()
    {
        return HttpRequest.CreatePostRequest($"collections/{this.CollectionId}/delete", this);
    }

    #region private ================================================================================

    private DeleteEmbeddingsRequest(string collectionId, string[] ids)
    {
        this.CollectionId = collectionId;
        this.Ids = ids;
    }

    #endregion
}
