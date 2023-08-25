// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.MemoryStorage;
using Microsoft.SemanticMemory.MemoryStorage.Volitile;

// ReSharper disable once CheckNamespace
namespace Microsoft.SemanticMemory;

public static partial class MemoryClientBuilderExtensions
{
    public static MemoryClientBuilder WithVolitileMemory(this MemoryClientBuilder builder)
    {
        builder.Services.AddVolitileMemory();

        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddVolitileMemory(this IServiceCollection services)
    {
        return services
            .AddSingleton<ISemanticMemoryVectorDb, VolitileMemory>();
    }
}
