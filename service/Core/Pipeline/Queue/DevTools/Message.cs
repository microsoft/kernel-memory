// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.Pipeline.Queue.DevTools;

internal class Message
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("deliveries")]
    public uint DequeueCount { get; set; } = 0;

    [JsonPropertyName("created")]
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("schedule")]
    public DateTimeOffset Schedule { get; set; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("lock")]
    public DateTimeOffset LockedUntil { get; set; } = DateTimeOffset.MinValue;

    [JsonPropertyName("error")]
    public string LastError { get; set; } = string.Empty;

    public bool IsLocked()
    {
        return this.LockedUntil > DateTimeOffset.UtcNow;
    }

    public bool IsTimeToRun()
    {
        return this.Schedule <= DateTimeOffset.UtcNow;
    }

    public void RunIn(TimeSpan delay)
    {
        this.Schedule = DateTimeOffset.UtcNow + delay;
    }

    public void Lock(int seconds)
    {
        this.LockedUntil = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(Math.Max(0, seconds));
    }

    public void Unlock()
    {
        this.LockedUntil = DateTimeOffset.MinValue;
    }
}
