// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.SemanticMemory.Core.MemoryStorage.Qdrant;

public static partial class DependencyInjection
{
    public static IServiceCollection AddQdrantAsVectorDb(this IServiceCollection services, QdrantConfig config)
    {
        return services
            .AddSingleton<QdrantConfig>(config)
            .AddSingleton<ISemanticMemoryVectorDb, QdrantMemory>()
            .AddSingleton<QdrantMemory, QdrantMemory>();
    }
}
