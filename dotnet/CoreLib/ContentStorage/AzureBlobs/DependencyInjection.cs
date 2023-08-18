// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.Core.AppBuilders;

namespace Microsoft.SemanticMemory.Core.ContentStorage.AzureBlobs;

public static class MemoryClientBuilderExtensions
{
    public static MemoryClientBuilder WithAzureBlobsStorage(this MemoryClientBuilder builder, AzureBlobsConfig config)
    {
        builder.Services.AddAzureBlobAsContentStorage(config);
        return builder;
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddAzureBlobAsContentStorage(this IServiceCollection services, AzureBlobsConfig config)
    {
        return services
            .AddSingleton<AzureBlobsConfig>(config)
            .AddSingleton<IContentStorage, AzureBlobsStorage>();
    }
}
