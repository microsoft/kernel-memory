// Copyright (c) Free Mind Labs, Inc. All rights reserved.

using Elastic.Clients.Elasticsearch;
using FreeMindLabs.KernelMemory.Elasticsearch.Extensions;
using Microsoft.KernelMemory.Elasticsearch;
using Microsoft.KernelMemory.MemoryStorage;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for KernelMemoryBuilder and generic DI
/// </summary>
public static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// Inject Elasticsearch as the default implementation of IMemoryDb
    /// </summary>
    public static IServiceCollection AddElasticsearchAsVectorDb(this IServiceCollection services,
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
            .AddSingleton<IIndexNameHelper, IndexNameHelper>()
            .AddSingleton(esConfig)
            .AddSingleton<IMemoryDb, ElasticsearchMemory>();
    }

    /// <summary>
    /// Inject Elasticsearch as the default implementation of IMemoryDb
    /// </summary>
    public static IServiceCollection AddElasticsearchAsVectorDb(this IServiceCollection services,
               Action<ElasticsearchConfigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure, nameof(configure));

        var cfg = new ElasticsearchConfigBuilder();
        configure(cfg);

        return services.AddElasticsearchAsVectorDb(cfg.Build());
    }
}
