// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.AI.Anthropic.Client;

internal sealed class Delta
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = null!;
}
