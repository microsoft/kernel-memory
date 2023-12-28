// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.Orchestration.AzureQueues;
using Microsoft.KernelMemory.Pipeline.Queue;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithAzureQueuesOrchestration(this IKernelMemoryBuilder builder, AzureQueuesConfig config)
    {
        builder.Services.AddAzureQueuesOrchestration(config);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureQueuesOrchestration(this IServiceCollection services, AzureQueuesConfig config)
    {
        IQueue QueueFactory(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<AzureQueuesPipeline>()
                   ?? throw new KernelMemoryException("Unable to instantiate " + typeof(AzureQueuesPipeline));
        }

        // The orchestrator uses multiple queue clients, each linked to a specific queue,
        // so it requires a factory rather than a single queue injected to the ctor.
        return services
            .AddSingleton<AzureQueuesConfig>(config)
            .AddTransient<AzureQueuesPipeline>()
            .AddSingleton<QueueClientFactory>(serviceProvider => new QueueClientFactory(() => QueueFactory(serviceProvider)));
    }
}
