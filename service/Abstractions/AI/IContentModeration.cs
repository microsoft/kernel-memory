// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.KernelMemory.AI;

[Experimental("KMEXP05")]
public interface IContentModeration
{
    /// <summary>
    /// Check if the input text is safe
    /// </summary>
    /// <param name="text">Input text</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>True if the input text is safe</returns>
    public Task<bool> IsSafeAsync(string? text, CancellationToken cancellationToken);

    /// <summary>
    /// Check if the input text is safe
    /// </summary>
    /// <param name="text">Input text</param>
    /// <param name="threshold">Safety threshold</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>True if the input text is safe</returns>
    public Task<bool> IsSafeAsync(string? text, double threshold, CancellationToken cancellationToken);
}
