// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Elastic.Clients.Elasticsearch;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.MemoryDb.Elasticsearch;

namespace Microsoft.KernelMemory;

/// <summary>
/// DI pipelines for Elasticsearch Memory.
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    /// <summary>
    /// Kernel Memory Builder extension method to add the Elasticsearch memory db.
    /// </summary>
    /// <param name="builder">The IKernelMemoryBuilder instance</param>
    /// <param name="configuration">The application configuration</param>"
    public static IKernelMemoryBuilder WithElasticsearchMemoryDb(this IKernelMemoryBuilder builder,
        ElasticsearchConfig configuration)
    {
        builder.Services.AddElasticsearchAsMemoryDb(configuration);

        return builder;
    }

    /// <summary>
    /// Extension method to add the Elasticsearch memory db.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IKernelMemoryBuilder WithElasticsearch(this IKernelMemoryBuilder builder,
        Action<ElasticsearchConfigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure, nameof(configure));

        var cfg = new ElasticsearchConfigBuilder();
        configure(cfg);

        builder.Services.AddElasticsearchAsMemoryDb(cfg.Build());
        return builder;
    }
}

/// <summary>
/// Setup Elasticsearch memory within the semantic kernel.
/// </summary>
public static partial class DependencyInjection
{
    /// <summary>
    /// Inject Elasticsearch as the default implementation of IMemoryDb
    /// </summary>
    public static IServiceCollection AddElasticsearchAsMemoryDb(this IServiceCollection services,
        ElasticsearchConfig esConfig)
    {
        ArgumentNullException.ThrowIfNull(esConfig, nameof(esConfig));

        // The ElasticsearchClient type is thread-safe and can be shared and
        // reused across multiple threads in consuming applications. 
        // See https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/recommendations.html
        services.AddSingleton(sp =>
        {
            var esConfig = sp.GetRequiredService<ElasticsearchConfig>();
            return new ElasticsearchClient(esConfig.ToElasticsearchClientSettings());
        });

        return services
            .AddSingleton(esConfig)
            .AddSingleton<IMemoryDb, ElasticsearchMemory>();
    }

    /// <summary>
    /// Inject Elasticsearch as the default implementation of IMemoryDb
    /// </summary>
    public static IServiceCollection AddElasticsearchAsMemoryDb(this IServiceCollection services,
               Action<ElasticsearchConfigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure, nameof(configure));

        var cfg = new ElasticsearchConfigBuilder();
        configure(cfg);

        return services.AddElasticsearchAsMemoryDb(cfg.Build());
    }
}
