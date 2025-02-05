// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory;

/// <summary>
/// Represents the usage of tokens in a request and response cycle.
/// </summary>
public class TokenUsage
{
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("serviceType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ServiceType { get; set; }

    [JsonPropertyName("modelType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ModelType { get; set; }

    [JsonPropertyName("modelName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ModelName { get; set; }

    /// <summary>
    /// The number of tokens in the request message input, spanning all message content items, measured by the tokenizer.
    /// </summary>
    [JsonPropertyName("tokenizerTokensIn")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TokenizerTokensIn { get; set; }

    /// <summary>
    /// The combined number of output tokens in the generated completion, measured by the tokenizer.
    /// </summary>
    [JsonPropertyName("tokenizerTokensOut")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TokenizerTokensOut { get; set; }

    /// <summary>
    /// The number of tokens in the request message input, spanning all message content items, measured by the service.
    /// </summary>
    [JsonPropertyName("serviceTokensIn")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ServiceTokensIn { get; set; }

    /// <summary>
    /// The combined number of output tokens in the generated completion, as consumed by the model.
    /// </summary>
    [JsonPropertyName("serviceTokensOut")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ServiceTokensOut { get; set; }

    [JsonPropertyName("serviceReasoningTokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ServiceReasoningTokens { get; set; }

    public TokenUsage()
    {
    }

    public void Merge(TokenUsage? input)
    {
        if (input == null)
        {
            return;
        }

        this.Timestamp = input.Timestamp;
        this.ServiceType = input.ServiceType;
        this.ModelType = input.ModelType;
        this.ModelName = input.ModelName;

        this.TokenizerTokensIn = (this.TokenizerTokensIn ?? 0) + (input.TokenizerTokensIn ?? 0);
        this.TokenizerTokensOut = (this.TokenizerTokensOut ?? 0) + (input.TokenizerTokensOut ?? 0);
        this.ServiceTokensIn = (this.ServiceTokensIn ?? 0) + (input.ServiceTokensIn ?? 0);
        this.ServiceTokensOut = (this.ServiceTokensOut ?? 0) + (input.ServiceTokensOut ?? 0);
        this.ServiceReasoningTokens = (this.ServiceReasoningTokens ?? 0) + (input.ServiceReasoningTokens ?? 0);
    }
}
