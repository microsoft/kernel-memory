// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Pipeline.Queue;
using Microsoft.SemanticMemory.Pipeline.Queue.DevTools;

// ReSharper disable once CheckNamespace
namespace Microsoft.SemanticMemory;

public static partial class MemoryClientBuilderExtensions
{
    public static MemoryClientBuilder WithSimpleQueuesPipeline(this MemoryClientBuilder builder, string path)
    {
        return builder.WithSimpleQueuesPipeline(new SimpleQueuesConfig { Directory = path });
    }

    public static MemoryClientBuilder WithSimpleQueuesPipeline(this MemoryClientBuilder builder, SimpleQueuesConfig config)
    {
        builder.Services.AddSimpleQueues(config);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddSimpleQueues(this IServiceCollection services, string path)
    {
        return services.AddSimpleQueues(new SimpleQueuesConfig { Directory = path });
    }

    public static IServiceCollection AddSimpleQueues(this IServiceCollection services, SimpleQueuesConfig config)
    {
        IQueue QueueFactory(IServiceProvider serviceProvider)
        {
            return new SimpleQueues(config, log: serviceProvider.GetService<ILogger<SimpleQueues>>());
        }

        // The orchestrator uses multiple queue clients, each linked to a specific queue,
        // so it requires a factory rather than a single queue injected to the ctor.
        return services
            .AddSingleton<SimpleQueuesConfig>(config)
            .AddTransient<SimpleQueues>()
            .AddSingleton<QueueClientFactory>(serviceProvider => new QueueClientFactory(() => QueueFactory(serviceProvider)));
    }
}
