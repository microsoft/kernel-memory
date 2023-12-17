// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.ContentStorage.AzureBlobs;
using Microsoft.KernelMemory;

namespace Options;

public class ServicesOptions
{
    public AzureBlobsConfig AzureBlobsConfig { get; set; } = default!;

    public AzureOpenAIConfig AzureOpenAIText { get; set; } = default!;

    public AzureOpenAIConfig AzureOpenAIEmbedding { get; set; } = default!;

    public AzureAISearchConfig AzureAISearchConfig { get; set; } = default!;
}
