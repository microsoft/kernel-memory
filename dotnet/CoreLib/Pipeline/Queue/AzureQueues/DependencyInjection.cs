// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Core.Configuration;

namespace Microsoft.SemanticMemory.Core.Pipeline.Queue.AzureQueues;

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureQueue(this IServiceCollection services, AzureQueueConfig config)
    {
        AzureQueueConfig configCopy = JsonSerializer.Deserialize<AzureQueueConfig>(JsonSerializer.Serialize(config))
                                      ?? throw new ConfigurationException("Unable to copy Azure Queue configuration");

        IQueue QueueFactory(IServiceProvider serviceProvider)
        {
            return new AzureQueue(configCopy, serviceProvider.GetService<ILogger<AzureQueue>>());
        }

        // The orchestrator uses multiple queue clients, each linked to a specific queue,
        // so it requires a factory rather than a single queue injected to the ctor.
        return services.AddSingleton<QueueClientFactory>(
            serviceProvider => new QueueClientFactory(() => QueueFactory(serviceProvider)));
    }
}
