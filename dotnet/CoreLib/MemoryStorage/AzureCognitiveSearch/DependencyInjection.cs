// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.Client;
using Microsoft.SemanticMemory.Core.AppBuilders;

namespace Microsoft.SemanticMemory.Core.MemoryStorage.AzureCognitiveSearch;

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureCognitiveSearchAsVectorDb(this IServiceCollection services, AzureCognitiveSearchConfig config)
    {
        return services
            .AddSingleton<AzureCognitiveSearchConfig>(config)
            .AddSingleton<ISemanticMemoryVectorDb, AzureCognitiveSearchMemory>()
            .AddSingleton<AzureCognitiveSearchMemory, AzureCognitiveSearchMemory>();
    }

    public static void AddAzureCognitiveSearchAsVectorDbToList(this ConfiguredServices<ISemanticMemoryVectorDb> services, AzureCognitiveSearchConfig config)
    {
        services.Add(serviceProvider => serviceProvider.GetService<AzureCognitiveSearchMemory>()
                                        ?? throw new SemanticMemoryException("Unable to instantiate " + typeof(AzureCognitiveSearchMemory)));
    }
}
