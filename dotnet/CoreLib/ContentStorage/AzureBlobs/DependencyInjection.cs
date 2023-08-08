// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticMemory.Core.Configuration;

namespace Microsoft.SemanticMemory.Core.ContentStorage.AzureBlobs;

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureBlobAsContentStorage(this IServiceCollection services, AzureBlobConfig config)
    {
        AzureBlobConfig configCopy = JsonSerializer.Deserialize<AzureBlobConfig>(JsonSerializer.Serialize(config))
                                     ?? throw new ConfigurationException("Unable to copy Azure Blob configuration");

        return services
            .AddSingleton<IContentStorage>(serviceProvider => new AzureBlob(configCopy, serviceProvider.GetService<ILogger<AzureBlob>>()))
            .AddSingleton<AzureBlob>(serviceProvider => new AzureBlob(configCopy, serviceProvider.GetService<ILogger<AzureBlob>>()));
    }

    public static void AddAzureBlobAsContentStorageToList(this ConfiguredServices<IContentStorage> services, AzureBlobConfig config)
    {
        AzureBlobConfig configCopy = JsonSerializer.Deserialize<AzureBlobConfig>(JsonSerializer.Serialize(config))
                                     ?? throw new ConfigurationException("Unable to copy Azure Blob configuration");

        services.Add(serviceProvider => new AzureBlob(configCopy, serviceProvider.GetService<ILogger<AzureBlob>>()));
    }
}
