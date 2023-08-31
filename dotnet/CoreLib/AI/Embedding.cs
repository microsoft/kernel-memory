﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel.AI.Embeddings.VectorOperations;

#pragma warning disable IDE0130 // first class concept we want to have readily available
#pragma warning disable CA2225 // no need for explicit methods

// ReSharper disable once CheckNamespace
namespace Microsoft.SemanticMemory;

/// <summary>
/// Note: use Embedding.JsonConverter to serialize objects using this type.
/// Example:
///     [JsonPropertyName("vector")]
///     [JsonConverter(typeof(Embedding.JsonConverter))]
///     public Embedding Vector { get; set; }
/// </summary>
public struct Embedding : IEquatable<Embedding>
{
    /// <summary>
    /// Note: use Embedding.JsonConverter to serialize objects using this type.
    /// </summary>
    [JsonIgnore]
    public ReadOnlyMemory<float> Data { get; set; } = new();

    /// <summary>
    /// Note: use Embedding.JsonConverter to serialize objects using this type.
    /// </summary>
    [JsonIgnore]
    public int Length => this.Data.Length;

    public Embedding(float[] vector)
    {
        this.Data = vector;
    }

    public Embedding(ReadOnlyMemory<float> vector)
    {
        this.Data = vector;
    }

    public Embedding(int size)
    {
        this.Data = new ReadOnlyMemory<float>(new float[size]);
    }

    public double CosineSimilarity(Embedding embedding)
    {
        return this.Data.ToArray().CosineSimilarity(embedding.Data.ToArray());
    }

    /// <summary>
    /// Convert Semantic Kernel data type
    /// </summary>
    public static implicit operator Embedding(ReadOnlyMemory<float> data) => new(data);

    /// <summary>
    /// Allows simple embedding definition using float[]
    /// </summary>
    public static implicit operator Embedding(float[] data) => new(data);

    public bool Equals(Embedding other) => this.Data.Equals(other.Data);

    public override bool Equals(object? obj) => (obj is Embedding other && this.Equals(other));

    public static bool operator ==(Embedding v1, Embedding v2) => v1.Equals(v2);

    public static bool operator !=(Embedding v1, Embedding v2) => !(v1 == v2);

    public override int GetHashCode() => this.Data.GetHashCode();

    /// <summary>
    /// Note: use Embedding.JsonConverter to serialize objects using
    /// the Embedding type, for example:
    ///     [JsonPropertyName("vector")]
    ///     [JsonConverter(typeof(Embedding.JsonConverter))]
    ///     public Embedding Vector { get; set; }
    /// </summary>
    public sealed class JsonConverter : JsonConverter<Embedding>
    {
        /// <summary>An instance of a converter for float[] that all operations delegate to</summary>
        private static readonly JsonConverter<float[]> s_converter =
            (JsonConverter<float[]>)new JsonSerializerOptions().GetConverter(typeof(float[]));

        public override Embedding Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new Embedding(s_converter.Read(ref reader, typeof(float[]), options) ?? Array.Empty<float>());
        }

        public override void Write(Utf8JsonWriter writer, Embedding value, JsonSerializerOptions options)
        {
            s_converter.Write(writer, MemoryMarshal.TryGetArray(value.Data, out ArraySegment<float> array) && array.Count == value.Length
                ? array.Array!
                : value.Data.ToArray(), options);
        }
    }
}
