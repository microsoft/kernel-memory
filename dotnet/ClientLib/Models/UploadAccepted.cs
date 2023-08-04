// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.SemanticMemory.Client.Models;

public class UploadAccepted
{
    [JsonPropertyName("UserId")]
    [JsonPropertyOrder(1)]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("Id")]
    [JsonPropertyOrder(2)]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Message")]
    [JsonPropertyOrder(3)]
    public string Message { get; set; } = string.Empty;
}
