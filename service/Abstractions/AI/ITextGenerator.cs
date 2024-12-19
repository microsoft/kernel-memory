// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.KernelMemory.AI;

public interface ITextGenerator : ITextTokenizer
{
    /// <summary>
    /// Max size of the LLM attention window, considering both input and output tokens.
    /// </summary>
    public int MaxTokenTotal { get; }

    /// <summary>
    /// Generate text for the given prompt, aka generate a text completion.
    /// </summary>
    /// <param name="prompt">Prompt text</param>
    /// <param name="options">Options for the LLM request</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Text generated, returned as a stream of strings/tokens</returns>
    public IAsyncEnumerable<GeneratedTextContent> GenerateTextAsync(
        string prompt,
        TextGenerationOptions options,
        CancellationToken cancellationToken = default);
}
