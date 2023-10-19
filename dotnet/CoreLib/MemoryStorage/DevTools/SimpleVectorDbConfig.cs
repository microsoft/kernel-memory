// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.FileSystem.DevTools;

namespace Microsoft.SemanticMemory.MemoryStorage.DevTools;

public class SimpleVectorDbConfig
{
    /// <summary>
    /// The type of storage to use. Defaults to volatile (in RAM).
    /// </summary>
    public FileSystemTypes StorageType { get; set; } = FileSystemTypes.Volatile;

    /// <summary>
    /// Directory of the text file storage.
    /// </summary>
    public string Directory { get; set; } = "tmp-memory-vectors";
}
