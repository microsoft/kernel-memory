// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.Core.ContentStorage.AzureBlobs;
using Microsoft.SemanticMemory.Core.ContentStorage.FileSystem;

namespace Microsoft.SemanticMemory.Core.ContentStorage;

public class ContentStorageConfig
{
    public string Type { get; set; } = "filesystem";
    public FileSystemConfig FileSystem { get; set; } = new();
    public AzureBlobsConfig AzureBlobs { get; set; } = new();
}
