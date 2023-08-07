// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Core.AppBuilders;

namespace Microsoft.SemanticMemory.Core.ContentStorage.AzureBlobs;

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureBlobAsContentStorage(this IServiceCollection services, AzureBlobConfig config)
    {
        switch (config.Auth)
        {
            case "":
            case string x when x.Equals("AzureIdentity", StringComparison.OrdinalIgnoreCase):
                return services
                    .AddSingleton<IContentStorage>(serviceProvider => new AzureBlob(
                        accountName: config.Account,
                        endpointSuffix: config.EndpointSuffix,
                        containerName: config.Container,
                        logger: serviceProvider.GetService<ILogger<AzureBlob>>()))
                    .AddSingleton<AzureBlob>(serviceProvider => new AzureBlob(
                        accountName: config.Account,
                        endpointSuffix: config.EndpointSuffix,
                        containerName: config.Container,
                        logger: serviceProvider.GetService<ILogger<AzureBlob>>()));

            case string x when x.Equals("ConnectionString", StringComparison.OrdinalIgnoreCase):
                return services
                    .AddSingleton<IContentStorage>(serviceProvider => new AzureBlob(
                        connectionString: config.ConnectionString,
                        containerName: config.Container,
                        logger: serviceProvider.GetService<ILogger<AzureBlob>>()))
                    .AddSingleton<AzureBlob>(serviceProvider => new AzureBlob(
                        connectionString: config.ConnectionString,
                        containerName: config.Container,
                        logger: serviceProvider.GetService<ILogger<AzureBlob>>()));

            default:
                throw new NotImplementedException($"Azure Blob auth type '{config.Auth}' not available");
        }
    }

    public static void AddAzureBlobAsContentStorageToList(this ConfiguredServices<IContentStorage> services, AzureBlobConfig config)
    {
        switch (config.Auth)
        {
            case "":
            case string x when x.Equals("AzureIdentity", StringComparison.OrdinalIgnoreCase):
                services.Add(serviceProvider => new AzureBlob(
                    accountName: config.Account,
                    endpointSuffix: config.EndpointSuffix,
                    containerName: config.Container,
                    logger: serviceProvider.GetService<ILogger<AzureBlob>>()));
                break;

            case string x when x.Equals("ConnectionString", StringComparison.OrdinalIgnoreCase):
                services.Add(serviceProvider => new AzureBlob(
                    connectionString: config.ConnectionString,
                    containerName: config.Container,
                    logger: serviceProvider.GetService<ILogger<AzureBlob>>()));
                break;

            default:
                throw new NotImplementedException($"Azure Blob auth type '{config.Auth}' not available");
        }
    }
}
