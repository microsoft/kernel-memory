// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.FileSystem.DevTools;

namespace Microsoft.SemanticMemory.ContentStorage.DevTools;

public class SimpleFileStorageConfig
{
    /// <summary>
    /// The type of storage to use. Defaults to volatile (in RAM).
    /// </summary>
    public FileSystemTypes StorageType { get; set; } = FileSystemTypes.Volatile;

    public string Directory { get; set; } = "tmp-memory-files";
}
