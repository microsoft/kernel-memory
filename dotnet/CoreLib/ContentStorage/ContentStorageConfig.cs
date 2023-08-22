// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.ContentStorage.AzureBlobs;
using Microsoft.SemanticMemory.ContentStorage.FileSystem;

namespace Microsoft.SemanticMemory.ContentStorage;

public class ContentStorageConfig
{
    public string Type { get; set; } = "filesystem";
    public FileSystemConfig FileSystem { get; set; } = new();
    public AzureBlobsConfig AzureBlobs { get; set; } = new();
}
