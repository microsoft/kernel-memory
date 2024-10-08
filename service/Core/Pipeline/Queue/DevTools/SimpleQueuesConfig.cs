// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.KernelMemory.FileSystem.DevTools;

namespace Microsoft.KernelMemory.Pipeline.Queue.DevTools;

public class SimpleQueuesConfig
{
    public static SimpleQueuesConfig Volatile { get => new() { StorageType = FileSystemTypes.Volatile }; }

    public static SimpleQueuesConfig Persistent { get => new() { StorageType = FileSystemTypes.Disk }; }

    /// <summary>
    /// The type of storage to use. Defaults to volatile (in RAM).
    /// </summary>
    public FileSystemTypes StorageType { get; set; } = FileSystemTypes.Volatile;

    /// <summary>
    /// Messages storage directory
    /// </summary>
    public string Directory { get; set; } = "tmp-memory-queues";

    /// <summary>
    /// How often to check if there are new messages.
    /// </summary>
    public int PollDelayMsecs { get; set; } = 100;

    /// <summary>
    /// How often to dispatch messages in the queue.
    /// </summary>
    public int DispatchFrequencyMsecs { get; set; } = 100;

    /// <summary>
    /// How many messages to fetch at a time.
    /// </summary>
    public int FetchBatchSize { get; set; } = 3;

    /// <summary>
    /// How long to lock messages once fetched.
    /// </summary>
    public int FetchLockSeconds { get; set; } = 300;

    /// <summary>
    /// How many times to retry processing a failing message.
    /// Example: a value of 20 means that a message will be processed up to 21 times.
    /// </summary>
    public int MaxRetriesBeforePoisonQueue { get; set; } = 1;

    /// <summary>
    /// Suffix used for the poison queue directories
    /// </summary>
    public string PoisonQueueSuffix { get; set; } = "-poison";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(this.Directory) || this.Directory.Contains(' ', StringComparison.OrdinalIgnoreCase))
        {
            throw new ConfigurationException($"SimpleQueue: {nameof(this.Directory)} cannot be empty or have leading or trailing spaces");
        }

        if (string.IsNullOrWhiteSpace(this.PoisonQueueSuffix) || this.PoisonQueueSuffix.Contains(' ', StringComparison.OrdinalIgnoreCase))
        {
            throw new ConfigurationException($"SimpleQueue: {nameof(this.PoisonQueueSuffix)} cannot be empty or have leading or trailing spaces");
        }

        if (this.PollDelayMsecs < 1)
        {
            throw new ConfigurationException($"SimpleQueue: {nameof(this.PollDelayMsecs)} value {this.PollDelayMsecs} is too low, cannot be less than 1");
        }

        if (this.DispatchFrequencyMsecs < 1)
        {
            throw new ConfigurationException($"SimpleQueue: {nameof(this.DispatchFrequencyMsecs)} value {this.DispatchFrequencyMsecs} is too low, cannot be less than 1");
        }

        if (this.FetchBatchSize < 1)
        {
            throw new ConfigurationException($"SimpleQueue: {nameof(this.FetchBatchSize)} value {this.FetchBatchSize} is too low, cannot be less than 1");
        }

        if (this.FetchLockSeconds < 1)
        {
            throw new ConfigurationException($"SimpleQueue: {nameof(this.FetchLockSeconds)} value {this.FetchLockSeconds} is too low, cannot be less than 1");
        }

        if (this.MaxRetriesBeforePoisonQueue < 0)
        {
            throw new ConfigurationException($"SimpleQueue: {nameof(this.MaxRetriesBeforePoisonQueue)} value {this.MaxRetriesBeforePoisonQueue} is too low, cannot be less than 0");
        }
    }
}
