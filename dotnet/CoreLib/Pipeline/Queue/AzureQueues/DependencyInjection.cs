// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.Client;

namespace Microsoft.SemanticMemory.Core.Pipeline.Queue.AzureQueues;

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureQueue(this IServiceCollection services, AzureQueueConfig config)
    {
        IQueue QueueFactory(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<AzureQueue>()
                   ?? throw new SemanticMemoryException("Unable to instantiate " + typeof(AzureQueue));
        }

        // The orchestrator uses multiple queue clients, each linked to a specific queue,
        // so it requires a factory rather than a single queue injected to the ctor.
        return services
            .AddSingleton<AzureQueueConfig>(config)
            .AddTransient<AzureQueue>()
            .AddSingleton<QueueClientFactory>(serviceProvider => new QueueClientFactory(() => QueueFactory(serviceProvider)));
    }
}
