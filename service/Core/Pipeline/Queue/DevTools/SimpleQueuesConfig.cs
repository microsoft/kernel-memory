// Copyright (c) Microsoft. All rights reserved.

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

    public string Directory { get; set; } = "tmp-memory-queues";
}
