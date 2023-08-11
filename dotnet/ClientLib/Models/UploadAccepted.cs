// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.SemanticMemory.Client.Models;

public class UploadAccepted
{
    [JsonPropertyName("userId")]
    [JsonPropertyOrder(1)]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("documentId")]
    [JsonPropertyOrder(2)]
    public string DocumentId { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    [JsonPropertyOrder(3)]
    public string Message { get; set; } = string.Empty;
}
