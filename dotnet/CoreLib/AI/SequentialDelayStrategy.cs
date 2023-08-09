// Copyright (c) Microsoft. All rights reserved.

using System;
using Azure;
using Azure.Core;

namespace Microsoft.SemanticMemory.Core.AI;

public class SequentialDelayStrategy : DelayStrategy
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

    public SequentialDelayStrategy() : base(s_maxDelay, 0)
    {
    }

    protected override TimeSpan GetNextDelayCore(Response? response, int retryNumber)
    {
        int index = retryNumber - 1;
        return index >= s_pollingSequence.Length ? s_maxDelay : s_pollingSequence[index];
    }
}
