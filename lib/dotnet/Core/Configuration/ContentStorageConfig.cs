// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.Core.ContentStorage;

namespace Microsoft.SemanticMemory.Core.Configuration;

public class ContentStorageConfig
{
    public string Type { get; set; } = "filesystem";
    public FileSystemConfig FileSystem { get; set; } = new();
    public AzureBlobConfig AzureBlobs { get; set; } = new();
}
