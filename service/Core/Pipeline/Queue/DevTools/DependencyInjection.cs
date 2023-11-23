// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.Pipeline.Queue;
using Microsoft.KernelMemory.Pipeline.Queue.DevTools;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithSimpleQueuesPipeline(this IKernelMemoryBuilder builder, SimpleQueuesConfig? config = null)
    {
        builder.Services.AddSimpleQueues(config ?? new SimpleQueuesConfig());
        return builder;
    }

    public static IKernelMemoryBuilder WithSimpleQueuesPipeline(this IKernelMemoryBuilder builder, string directory)
    {
        builder.Services.AddSimpleQueues(directory);
        return builder;
    }
}

public static partial class DependencyInjection
{
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

    public static IServiceCollection AddSimpleQueues(this IServiceCollection services, string directory)
    {
        return services.AddSimpleQueues(new SimpleQueuesConfig { StorageType = FileSystemTypes.Disk, Directory = directory });
    }
}
