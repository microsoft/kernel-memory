// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.MemoryDb.Qdrant.Client.Http;

internal sealed class CreateCollectionRequest
{
    internal sealed class VectorSettings
    {
        private readonly QdrantDistanceType _distanceType;

        [JsonPropertyName("size")]
        public int? Size { get; set; }

        [JsonPropertyName("distance")]
        public string DistanceAsString
        {
            get
            {
                return this._distanceType switch
                {
                    QdrantDistanceType.Cosine => "Cosine",
                    QdrantDistanceType.DotProduct => "DotProduct",
                    QdrantDistanceType.Euclidean => "Euclidean",
                    QdrantDistanceType.Manhattan => "Manhattan",
                    _ => throw new NotSupportedException($"Distance type {Enum.GetName(typeof(QdrantDistanceType), this._distanceType)} not supported")
                };
            }
        }

        public VectorSettings(int vectorSize, QdrantDistanceType distanceType)
        {
            this.Size = vectorSize;
            this._distanceType = distanceType;
        }

        internal void Validate()
        {
            ArgumentNullExceptionEx.ThrowIfNull(this.Size, nameof(this.Size), "The vector size cannot be null");
            ArgumentOutOfRangeExceptionEx.ThrowIfZeroOrNegative(this.Size!.Value, nameof(this.Size), "The vector size must be greater than zero");
            ArgumentNullExceptionEx.ThrowIfNull(this._distanceType, nameof(this._distanceType), "The distance type has not been defined");
            ArgumentOutOfRangeExceptionEx.ThrowIfNot(
                this._distanceType is QdrantDistanceType.Cosine or QdrantDistanceType.DotProduct or QdrantDistanceType.Euclidean or QdrantDistanceType.Manhattan,
                nameof(this._distanceType),
                $"Distance type {this._distanceType:G} not supported");
        }
    }

    private readonly string _collectionName;

    /// <summary>
    /// Collection settings consisting of a common vector length and vector distance calculation standard
    /// </summary>
    [JsonPropertyName("vectors")]
    public VectorSettings Settings { get; set; }

    public static CreateCollectionRequest Create(string collectionName, int vectorSize, QdrantDistanceType distanceType)
    {
        return new CreateCollectionRequest(collectionName, vectorSize, distanceType);
    }

    public HttpRequestMessage Build()
    {
        this.Settings.Validate();
        return HttpRequest.CreatePutRequest(
            $"collections/{this._collectionName}?wait=true",
            payload: this);
    }

    private CreateCollectionRequest(string collectionName, int vectorSize, QdrantDistanceType distanceType)
    {
        this._collectionName = collectionName;
        this.Settings = new VectorSettings(vectorSize, distanceType);
    }
}
