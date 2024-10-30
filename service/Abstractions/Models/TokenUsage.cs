// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.Models;

/// <summary>
/// Represents the usage of tokens in a request and response cycle.
/// </summary>
public class TokenUsage
{
    /// <summary>
    /// The number of tokens in the request message input, spanning all message content items.
    /// </summary>
    [JsonPropertyOrder(0)]
    public int InputTokenCount { get; set; }

    /// <summary>
    /// The combined number of output tokens in the generated completion, as consumed by the model.
    /// </summary>
    [JsonPropertyOrder(1)]
    public int OutputTokenCount { get; set; }

    /// <summary>
    /// The total number of combined input (prompt) and output (completion) tokens used.
    /// </summary>
    [JsonPropertyOrder(2)]
    public int TotalTokenCount { get; set; }
}
