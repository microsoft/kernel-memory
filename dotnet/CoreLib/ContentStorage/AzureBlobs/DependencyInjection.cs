// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;

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
}
