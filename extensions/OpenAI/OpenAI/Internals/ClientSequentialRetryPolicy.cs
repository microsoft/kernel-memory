// Copyright (c) Microsoft. All rights reserved.

using System;
using System.ClientModel.Primitives;

namespace Microsoft.KernelMemory.AI.OpenAI;

internal sealed class ClientSequentialRetryPolicy : ClientRetryPolicy
{
    private static readonly TimeSpan[] s_pollingSequence =
    {
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(6),
        TimeSpan.FromSeconds(6),
        TimeSpan.FromSeconds(8)
    };

    private static readonly TimeSpan s_maxDelay = s_pollingSequence[^1];

    public ClientSequentialRetryPolicy(int maxRetries = 3) : base(maxRetries)
    {
    }

    protected override TimeSpan GetNextDelay(PipelineMessage message, int tryCount)
    {
        int index = Math.Max(0, tryCount - 1);
        return index >= s_pollingSequence.Length ? s_maxDelay : s_pollingSequence[index];
    }
}
