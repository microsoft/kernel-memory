// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.AI.Anthropic.Client;

internal sealed class MessageResponse
{
    [JsonPropertyName("content")]
    public ContentResponse[]? Content { get; set; }
}
