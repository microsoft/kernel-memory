// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.MemoryDb.AzureCosmosDBMongoVCore;
using Microsoft.KernelMemory.MemoryStorage;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithAzureCosmosDBMongoVCoreMemoryDb(this IKernelMemoryBuilder builder, AzureCosmosDBMongoVCoreConfig config)
    {
        builder.Services.AddAzureCosmosDBMongoVCoreMemoryDb(config);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureCosmosDBMongoVCoreMemoryDb(this IServiceCollection services, AzureCosmosDBMongoVCoreConfig config)
    {
        return services
            .AddSingleton<AzureCosmosDBMongoVCoreConfig>(config)
            .AddSingleton<IMemoryDb, AzureCosmosDBMongoVCoreMemory>();
    }
}