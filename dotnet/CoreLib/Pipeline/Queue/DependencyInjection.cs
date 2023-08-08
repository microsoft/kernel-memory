// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Core.Pipeline.Queue.FileBasedQueues;

namespace Microsoft.SemanticMemory.Core.Pipeline.Queue;

public static partial class DependencyInjection
{
    public static IServiceCollection AddFileBasedQueue(this IServiceCollection services, FileBasedQueueConfig config)
    {
        IQueue QueueFactory(IServiceProvider serviceProvider)
        {
            return new FileBasedQueue(config, log: serviceProvider.GetService<ILogger<FileBasedQueue>>());
        }

        // The orchestrator uses multiple queue clients, each linked to a specific queue,
        // so it requires a factory rather than a single queue injected to the ctor.
        return services
            .AddSingleton<QueueClientFactory>(serviceProvider => new QueueClientFactory(() => QueueFactory(serviceProvider)));
    }
}
