// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.Search;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithDefaultSearchClient(
        this IKernelMemoryBuilder builder, SearchClientConfig? config = null)
    {
        config?.Validate();
        builder.Services.AddDefaultSearchClient(config ?? new());
        return builder;
    }

    public static IKernelMemoryBuilder WithSearchClientConfig(
        this IKernelMemoryBuilder builder, SearchClientConfig config)
    {
        config.Validate();
        builder.Services.AddSearchClientConfig(config);
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomSearchClient(
        this IKernelMemoryBuilder builder, ISearchClient instance)
    {
        builder.Services.AddCustomSearchClient(instance);
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomSearchClient<T>(
        this IKernelMemoryBuilder builder) where T : class, ISearchClient
    {
        builder.Services.AddCustomSearchClient<T>();
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddDefaultSearchClient(
        this IServiceCollection services, SearchClientConfig? config = null)
    {
        services.AddSingleton<SearchClientConfig>(config ?? new());
        return services.AddSingleton<ISearchClient, SearchClient>();
    }

    public static IServiceCollection AddCustomSearchClient(
        this IServiceCollection services, ISearchClient instance)
    {
        return services.AddSingleton<ISearchClient>(instance);
    }

    public static IServiceCollection AddCustomSearchClient<T>(
        this IServiceCollection services) where T : class, ISearchClient
    {
        return services.AddSingleton<ISearchClient, T>();
    }

    public static IServiceCollection AddSearchClientConfig(
        this IServiceCollection services, SearchClientConfig instance)
    {
        return services.AddSingleton<SearchClientConfig>(instance);
    }
}
