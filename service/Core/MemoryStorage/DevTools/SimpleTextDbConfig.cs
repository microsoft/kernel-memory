// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.FileSystem.DevTools;

namespace Microsoft.KernelMemory.MemoryStorage.DevTools;

public class SimpleTextDbConfig
{
    /// <summary>
    /// The type of storage to use. Defaults to volatile (in RAM).
    /// </summary>
    public FileSystemTypes StorageType { get; set; } = FileSystemTypes.Volatile;

    /// <summary>
    /// Directory of the text file storage.
    /// </summary>
    public string Directory { get; set; } = "tmp-memory-text";
}
