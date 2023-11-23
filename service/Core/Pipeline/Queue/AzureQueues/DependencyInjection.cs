// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.Pipeline.Queue;
using Microsoft.KernelMemory.Pipeline.Queue.AzureQueues;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithAzurequeuePipeline(this IKernelMemoryBuilder builder, AzureQueueConfig config)
    {
        builder.Services.AddAzureQueue(config);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureQueue(this IServiceCollection services, AzureQueueConfig config)
    {
        IQueue QueueFactory(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<AzureQueue>()
                   ?? throw new KernelMemoryException("Unable to instantiate " + typeof(AzureQueue));
        }

        // The orchestrator uses multiple queue clients, each linked to a specific queue,
        // so it requires a factory rather than a single queue injected to the ctor.
        return services
            .AddSingleton<AzureQueueConfig>(config)
            .AddTransient<AzureQueue>()
            .AddSingleton<QueueClientFactory>(serviceProvider => new QueueClientFactory(() => QueueFactory(serviceProvider)));
    }
}
