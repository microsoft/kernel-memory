// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.Core.ContentStorage.AzureBlobs;
using Microsoft.SemanticMemory.Core.ContentStorage.FileSystemStorage;

namespace Microsoft.SemanticMemory.Core.ContentStorage;

public class ContentStorageConfig
{
    public string Type { get; set; } = "filesystem";
    public FileSystemConfig FileSystem { get; set; } = new();
    public AzureBlobConfig AzureBlobs { get; set; } = new();
}
