// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.MemoryDb.AzureCosmosDBMongoDB;
using Microsoft.KernelMemory.MemoryStorage;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithAzureCosmosDBMongoDBMemoryDb(this IKernelMemoryBuilder builder, AzureCosmosDBMongoDBConfig config)
    {
        builder.Services.AddAzureCosmosDBMongoDBMemoryDb(config);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureCosmosDBMongoDBMemoryDb(this IServiceCollection services, AzureCosmosDBMongoDBConfig config)
    {
        return services
            .AddSingleton<AzureCosmosDBMongoDBConfig>(config)
            .AddSingleton<IMemoryDb, AzureCosmosDBMongoDBMemory>();
    }
}
