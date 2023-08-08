// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;

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
}
