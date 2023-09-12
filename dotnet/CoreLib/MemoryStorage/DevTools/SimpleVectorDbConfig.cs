// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticMemory.MemoryStorage.DevTools;

public class SimpleVectorDbConfig
{
    /// <summary>
    /// The type of storage to use.
    /// </summary>
    public enum StorageTypes
    {
        /// <summary>
        /// Save data to text files.
        /// </summary>
        TextFile,

        /// <summary>
        /// Save data to memory.
        /// </summary>
        Volatile,
    }

    /// <summary>
    /// Directory of the text file storage.
    /// </summary>
    public string Directory { get; set; } = "tmp-memory-vectors";

    /// <summary>
    /// The type of storage to use.
    /// </summary>
    public StorageTypes StorageType { get; set; } = StorageTypes.TextFile;
}
