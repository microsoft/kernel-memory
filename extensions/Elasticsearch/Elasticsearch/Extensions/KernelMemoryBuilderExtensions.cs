// Copyright (c) Free Mind Labs, Inc. All rights reserved.

using Microsoft.KernelMemory.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.KernelMemory;

/// <summary>
/// Extensions for KernelMemoryBuilder
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    /// <summary>
    /// Kernel Memory Builder extension method to add the Elasticsearch memory connector.
    /// </summary>
    /// <param name="builder">The IKernelMemoryBuilder instance</param>
    /// <param name="configuration">The application configuration</param>"
    public static IKernelMemoryBuilder WithElasticsearch(this IKernelMemoryBuilder builder,
        ElasticsearchConfig configuration)
    {
        builder.Services.AddElasticsearchAsVectorDb(configuration);

        return builder;
    }

    /// <summary>
    /// Extension method to add the Elasticsearch memory connector.
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

        builder.Services.AddElasticsearchAsVectorDb(cfg.Build());
        return builder;
    }
}
