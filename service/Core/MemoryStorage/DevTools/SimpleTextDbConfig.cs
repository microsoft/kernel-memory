// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.FileSystem.DevTools;

namespace Microsoft.KernelMemory.MemoryStorage.DevTools;

public class SimpleTextDbConfig
{
    public static SimpleTextDbConfig Volatile { get => new() { StorageType = FileSystemTypes.Volatile }; }

    public static SimpleTextDbConfig Persistent { get => new() { StorageType = FileSystemTypes.Disk }; }

    /// <summary>
    /// The type of storage to use. Defaults to volatile (in RAM).
    /// </summary>
    public FileSystemTypes StorageType { get; set; } = FileSystemTypes.Volatile;

    /// <summary>
    /// Directory of the text file storage.
    /// </summary>
    public string Directory { get; set; } = "tmp-memory-text";
}
