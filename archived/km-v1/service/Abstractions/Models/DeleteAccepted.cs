// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory;

public class DeleteAccepted
{
    [JsonPropertyName("index")]
    [JsonPropertyOrder(1)]
    public string Index { get; set; } = string.Empty;

    [JsonPropertyName("documentId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(2)]
    public string? DocumentId { get; set; } = null;

    [JsonPropertyName("message")]
    [JsonPropertyOrder(3)]
    public string Message { get; set; } = string.Empty;
}
