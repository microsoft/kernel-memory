// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.FileSystem.DevTools;

namespace Microsoft.SemanticMemory.Pipeline.Queue.DevTools;

public class SimpleQueuesConfig
{
    /// <summary>
    /// The type of storage to use. Defaults to volatile (in RAM).
    /// </summary>
    public FileSystemTypes StorageType { get; set; } = FileSystemTypes.Volatile;

    public string Directory { get; set; } = "tmp-memory-queues";
}
