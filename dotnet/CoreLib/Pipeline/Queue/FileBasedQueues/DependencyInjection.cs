// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Pipeline.Queue;
using Microsoft.SemanticMemory.Pipeline.Queue.FileBasedQueues;

// ReSharper disable once CheckNamespace
namespace Microsoft.SemanticMemory;

public static partial class MemoryClientBuilderExtensions
{
    public static MemoryClientBuilder WithFileBasedQueuePipeline(this MemoryClientBuilder builder, string path)
    {
        return builder.WithFileBasedQueuePipeline(new FileBasedQueueConfig { Path = path });
    }

    public static MemoryClientBuilder WithFileBasedQueuePipeline(this MemoryClientBuilder builder, FileBasedQueueConfig config)
    {
        builder.Services.AddFileBasedQueue(config);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddFileBasedQueue(this IServiceCollection services, string path)
    {
        return services.AddFileBasedQueue(new FileBasedQueueConfig { Path = path });
    }

    public static IServiceCollection AddFileBasedQueue(this IServiceCollection services, FileBasedQueueConfig config)
    {
        IQueue QueueFactory(IServiceProvider serviceProvider)
        {
            return new FileBasedQueue(config, log: serviceProvider.GetService<ILogger<FileBasedQueue>>());
        }

        // The orchestrator uses multiple queue clients, each linked to a specific queue,
        // so it requires a factory rather than a single queue injected to the ctor.
        return services
            .AddSingleton<FileBasedQueueConfig>(config)
            .AddTransient<FileBasedQueue>()
            .AddSingleton<QueueClientFactory>(serviceProvider => new QueueClientFactory(() => QueueFactory(serviceProvider)));
    }
}
