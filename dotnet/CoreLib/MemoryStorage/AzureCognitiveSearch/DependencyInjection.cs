// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.Core.AppBuilders;

namespace Microsoft.SemanticMemory.Core.MemoryStorage.AzureCognitiveSearch;

public static class MemoryClientBuilderExtensions
{
    public static MemoryClientBuilder WithAzureCognitiveSearch(this MemoryClientBuilder builder, AzureCognitiveSearchConfig config)
    {
        builder.Services.AddAzureCognitiveSearchAsVectorDb(config);
        return builder;
    }

    public static MemoryClientBuilder WithAzureCognitiveSearch(this MemoryClientBuilder builder, string endpoint, string apiKey)
    {
        builder.Services.AddAzureCognitiveSearchAsVectorDb(endpoint, apiKey);
        return builder;
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddAzureCognitiveSearchAsVectorDb(this IServiceCollection services, AzureCognitiveSearchConfig config)
    {
        return services
            .AddSingleton<AzureCognitiveSearchConfig>(config)
            .AddSingleton<ISemanticMemoryVectorDb, AzureCognitiveSearchMemory>();
    }

    public static IServiceCollection AddAzureCognitiveSearchAsVectorDb(this IServiceCollection services, string endpoint, string apiKey)
    {
        var config = new AzureCognitiveSearchConfig { Endpoint = endpoint, APIKey = apiKey, Auth = AzureCognitiveSearchConfig.AuthTypes.APIKey };
        return services.AddAzureCognitiveSearchAsVectorDb(config);
    }
}
