// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.DocumentStorage;

public class EmbeddingFileContent
{
    [JsonPropertyName("generator_name")]
    [JsonPropertyOrder(1)]
    public string GeneratorName { get; set; } = string.Empty;

    [JsonPropertyName("generator_provider")]
    [JsonPropertyOrder(2)]
    public string GeneratorProvider { get; set; } = string.Empty;

    [JsonPropertyName("vector_size")]
    [JsonPropertyOrder(3)]
    public int VectorSize { get; set; }

    [JsonPropertyName("source_file_name")]
    [JsonPropertyOrder(4)]
    public string SourceFileName { get; set; } = string.Empty;

    [JsonPropertyName("vector")]
    [JsonPropertyOrder(100)]
    [JsonConverter(typeof(Embedding.JsonConverter))]
    public Embedding Vector { get; set; }

    [JsonPropertyName("timestamp")]
    [JsonPropertyOrder(5)]
    public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
}
