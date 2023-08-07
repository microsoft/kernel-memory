// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Core.AppBuilders;

namespace Microsoft.SemanticMemory.Core.MemoryStorage.AzureCognitiveSearch;

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureCognitiveSearchAsVectorDb(this IServiceCollection services, AzureCognitiveSearchConfig config)
    {
        return services
            .AddSingleton<ISemanticMemoryVectorDb>(serviceProvider => new AzureCognitiveSearchMemory(
                endpoint: config.Endpoint,
                apiKey: config.APIKey,
                indexPrefix: config.VectorIndexPrefix,
                log: serviceProvider.GetService<ILogger<AzureCognitiveSearchMemory>>()))
            .AddSingleton<AzureCognitiveSearchMemory>(serviceProvider => new AzureCognitiveSearchMemory(
                endpoint: config.Endpoint,
                apiKey: config.APIKey,
                indexPrefix: config.VectorIndexPrefix,
                log: serviceProvider.GetService<ILogger<AzureCognitiveSearchMemory>>()));
    }

    public static void AddAzureCognitiveSearchAsVectorDbToList(this ConfiguredServices<ISemanticMemoryVectorDb> services, AzureCognitiveSearchConfig config)
    {
        services.Add(serviceProvider => new AzureCognitiveSearchMemory(
            endpoint: config.Endpoint,
            apiKey: config.APIKey,
            indexPrefix: config.VectorIndexPrefix,
            log: serviceProvider.GetService<ILogger<AzureCognitiveSearchMemory>>()));
    }
}
