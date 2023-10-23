// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.KernelMemory.AI;

public interface ITextGeneration
{
    public IAsyncEnumerable<string> GenerateTextAsync(
        string prompt,
        TextGenerationOptions options,
        CancellationToken cancellationToken = default);
}
