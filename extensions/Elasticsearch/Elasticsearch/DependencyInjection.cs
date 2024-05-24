// Copyright (c) Microsoft. All rights reserved.

using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.MemoryDb.Elasticsearch;
using Microsoft.KernelMemory.MemoryDb.Elasticsearch.Internals;
using Microsoft.KernelMemory.MemoryStorage;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// DI pipelines for Elasticsearch Memory.
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    /// <summary>
    /// Kernel Memory Builder extension method to add the Elasticsearch memory connector.
    /// </summary>
    /// <param name="builder">KM builder instance</param>
    /// <param name="configuration">Elasticsearch configuration</param>"
    public static IKernelMemoryBuilder WithElasticsearchMemoryDb(
        this IKernelMemoryBuilder builder,
        ElasticsearchConfig configuration)
    {
        builder.Services.AddElasticsearchAsMemoryDb(configuration);

        return builder;
    }

    /// <summary>
    /// Extension method to add the Elasticsearch memory connector.
    /// </summary>
    /// <param name="builder">KM builder instance</param>
    /// <param name="configure">Action to configure Elasticsearch</param>
    public static IKernelMemoryBuilder WithElasticsearch(
        this IKernelMemoryBuilder builder,
        Action<ElasticsearchConfigBuilder> configure)
    {
        ArgumentNullExceptionEx.ThrowIfNull(configure, nameof(configure), "The configure action is NULL");

        var cfg = new ElasticsearchConfigBuilder();
        configure(cfg);

        builder.Services.AddElasticsearchAsMemoryDb(cfg.Build());
        return builder;
    }
}

/// <summary>
/// Setup Elasticsearch memory within the DI service collection.
/// </summary>
public static partial class DependencyInjection
{
    /// <summary>
    /// Inject Elasticsearch as the default implementation of IMemoryDb
    /// </summary>
    public static IServiceCollection AddElasticsearchAsMemoryDb(
        this IServiceCollection services,
        ElasticsearchConfig config)
    {
        ArgumentNullExceptionEx.ThrowIfNull(config, nameof(config), "The configuration is NULL");

        // The ElasticsearchClient type is thread-safe and can be shared and
        // reused across multiple threads in consuming applications.
        // See https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/recommendations.html
        services.AddSingleton(sp =>
        {
            var esConfig = sp.GetRequiredService<ElasticsearchConfig>();
            return new ElasticsearchClient(esConfig.ToElasticsearchClientSettings());
        });

        return services
            .AddSingleton(config)
            .AddSingleton<IMemoryDb, ElasticsearchMemory>();
    }

    /// <summary>
    /// Inject Elasticsearch as the default implementation of IMemoryDb
    /// </summary>
    public static IServiceCollection AddElasticsearchAsMemoryDb(
        this IServiceCollection services,
        Action<ElasticsearchConfigBuilder> configure)
    {
        ArgumentNullExceptionEx.ThrowIfNull(configure, nameof(configure), "The configuration action is NULL");

        var cfg = new ElasticsearchConfigBuilder();
        configure(cfg);

        return services.AddElasticsearchAsMemoryDb(cfg.Build());
    }
}
