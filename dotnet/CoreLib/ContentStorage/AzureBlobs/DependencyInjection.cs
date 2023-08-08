// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.Client;
using Microsoft.SemanticMemory.Core.AppBuilders;

namespace Microsoft.SemanticMemory.Core.ContentStorage.AzureBlobs;

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureBlobAsContentStorage(this IServiceCollection services, AzureBlobConfig config)
    {
        return services
            .AddSingleton<AzureBlobConfig>(config)
            .AddSingleton<IContentStorage, AzureBlob>()
            .AddSingleton<AzureBlob, AzureBlob>();
    }

    public static void AddAzureBlobAsContentStorageToList(this ConfiguredServices<IContentStorage> services, AzureBlobConfig config)
    {
        services.Add(serviceProvider => serviceProvider.GetService<AzureBlob>()
                                        ?? throw new SemanticMemoryException("Unable to instantiate " + typeof(AzureBlob)));
    }
}
