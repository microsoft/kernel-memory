// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using Options;

namespace Extensions;

public static class KernelMemoryExtensions
{
    public static IServiceCollection AddKernelMemory(this IServiceCollection services)
    {
        services.AddSingleton(serviceProvider =>
        {
            var kernelMemoryOptions = serviceProvider.GetRequiredService<IOptions<KernelMemoryOptions>>();
            var serviceOptions = kernelMemoryOptions.Value.Services;
            var azblobConfig = serviceOptions.AzureBlobsConfig;
            var azAISearchConfig = serviceOptions.AzureAISearchConfig;
            var azAITextConfig = serviceOptions.AzureOpenAIText;
            var azAITextEmbeddingConfig = serviceOptions.AzureOpenAIEmbedding;

            return new KernelMemoryBuilder()
                .WithAzureBlobsStorage(azblobConfig)
                .WithAzureAISearch(azAISearchConfig)
                .WithAzureOpenAITextGeneration(azAITextConfig)
                .WithAzureOpenAITextEmbeddingGeneration(azAITextEmbeddingConfig)
                .Build<MemoryServerless>();
        });
        return services;
    }


}
