// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.AI.Anthropic.Client;

internal sealed class CallClaudeStreamingParams
{
    public CallClaudeStreamingParams(string modelName, string prompt)
    {
        this.ModelName = modelName;
        this.Prompt = prompt;
    }

    /// <summary>
    /// Name of the model
    /// </summary>
    public string ModelName { get; init; }

    public int MaxTokens { get; init; } = 2048;

    public string Prompt { get; init; }

    public string? System { get; init; }

    public double Temperature { get; init; } = 0;
}
