// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.Pipeline.Queue;

public struct QueueOptions : IEquatable<QueueOptions>
{
    public static readonly QueueOptions PubSub = new() { DequeueEnabled = true };
    public static readonly QueueOptions PublishOnly = new() { DequeueEnabled = false };

    public bool DequeueEnabled { get; set; } = true;

    public QueueOptions()
    {
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is QueueOptions options && this.Equals(options);
    }

    public readonly bool Equals(QueueOptions other)
    {
        return this.DequeueEnabled == other.DequeueEnabled;
    }

    public override readonly int GetHashCode()
    {
        return this.DequeueEnabled ? 1 : 2;
    }

    public static bool operator ==(QueueOptions obj1, QueueOptions obj2)
    {
        return obj1.Equals(obj2);
    }

    public static bool operator !=(QueueOptions obj1, QueueOptions obj2)
    {
        return !obj1.Equals(obj2);
    }
}
