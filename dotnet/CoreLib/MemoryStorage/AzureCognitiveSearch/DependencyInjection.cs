// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticMemory.Core.Configuration;

namespace Microsoft.SemanticMemory.Core.MemoryStorage.AzureCognitiveSearch;

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureCognitiveSearchAsVectorDb(this IServiceCollection services, AzureCognitiveSearchConfig config)
    {
        AzureCognitiveSearchConfig configCopy = JsonSerializer.Deserialize<AzureCognitiveSearchConfig>(JsonSerializer.Serialize(config))
                                                ?? throw new ConfigurationException("Unable to copy Azure Cognitive Search configuration");

        return services
            .AddSingleton<ISemanticMemoryVectorDb>(serviceProvider => new AzureCognitiveSearchMemory(
                configCopy,
                log: serviceProvider.GetService<ILogger<AzureCognitiveSearchMemory>>()))
            .AddSingleton<AzureCognitiveSearchMemory>(serviceProvider => new AzureCognitiveSearchMemory(
                configCopy,
                log: serviceProvider.GetService<ILogger<AzureCognitiveSearchMemory>>()));
    }

    public static void AddAzureCognitiveSearchAsVectorDbToList(this ConfiguredServices<ISemanticMemoryVectorDb> services, AzureCognitiveSearchConfig config)
    {
        AzureCognitiveSearchConfig configCopy = JsonSerializer.Deserialize<AzureCognitiveSearchConfig>(JsonSerializer.Serialize(config))
                                                ?? throw new ConfigurationException("Unable to copy Azure Cognitive Search configuration");

        services.Add(serviceProvider => new AzureCognitiveSearchMemory(
            configCopy,
            log: serviceProvider.GetService<ILogger<AzureCognitiveSearchMemory>>()));
    }
}
