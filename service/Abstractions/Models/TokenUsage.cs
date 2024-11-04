// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.Models;

/// <summary>
/// Represents the usage of tokens in a request and response cycle.
/// </summary>
public class TokenUsage
{
    public DateTime Timestamp { get; set; }

    public string? ServiceType { get; set; }

    public string? ModelType { get; set; }

    public string? ModelName { get; set; }

    /// <summary>
    /// The number of tokens in the request message input, spanning all message content items, measured by the tokenizer.
    /// </summary>
    [JsonPropertyName("tokenizer_tokens_in")]
    public int TokeninzerTokensIn { get; set; }

    /// <summary>
    /// The combined number of output tokens in the generated completion, measured by the tokenizer.
    /// </summary>
    [JsonPropertyName("tokenizer_tokens_out")]
    public int TokeninzerTokensOut { get; set; }

    /// <summary>
    /// The number of tokens in the request message input, spanning all message content items, measured by the service.
    /// </summary>
    [JsonPropertyName("service_tokens_in")]
    public int? ServiceTokensIn { get; set; }

    /// <summary>
    /// The combined number of output tokens in the generated completion, as consumed by the model.
    /// </summary>
    [JsonPropertyName("service_tokens_out")]
    public int? ServiceTokensOut { get; set; }

    [JsonPropertyName("service_reasoning_tokens")]
    public int? ServiceReasoningTokens { get; set; }
}
