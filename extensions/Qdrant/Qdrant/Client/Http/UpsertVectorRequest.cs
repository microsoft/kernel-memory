// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.MemoryDb.Qdrant.Client.Http;

internal sealed class UpsertVectorRequest<T> where T : DefaultQdrantPayload, new()
{
    internal sealed class BatchRequest
    {
        [JsonPropertyName("ids")]
        public List<Guid> Ids { get; set; } = new();

        [JsonPropertyName("vectors")]
        public List<Embedding> Vectors { get; set; } = new();

        [JsonPropertyName("payloads")]
        public List<T> Payloads { get; set; } = new();
    }

    private readonly string _collectionName;

    [JsonPropertyName("batch")]
    public BatchRequest Batch { get; set; }

    public static UpsertVectorRequest<T> Create(string collectionName)
    {
        return new UpsertVectorRequest<T>(collectionName);
    }

    public UpsertVectorRequest<T> UpsertVector(QdrantPoint<T> vectorRecord)
    {
        this.Batch.Ids.Add(vectorRecord.Id);
        this.Batch.Vectors.Add(vectorRecord.Vector);
        this.Batch.Payloads.Add(vectorRecord.Payload);
        return this;
    }

    public UpsertVectorRequest<T> UpsertRange(IEnumerable<QdrantPoint<T>> vectorRecords)
    {
        foreach (var vectorRecord in vectorRecords)
        {
            this.UpsertVector(vectorRecord);
        }

        return this;
    }

    public HttpRequestMessage Build()
    {
        return HttpRequest.CreatePutRequest(
            $"collections/{this._collectionName}/points?wait=true",
            payload: this);
    }

    private UpsertVectorRequest(string collectionName)
    {
        this._collectionName = collectionName;
        this.Batch = new BatchRequest();
    }
}
