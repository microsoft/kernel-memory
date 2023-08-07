// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.SemanticMemory.Core.Pipeline.Queue.RabbitMq;

public static partial class DependencyInjection
{
    public static IServiceCollection AddRabbitMq(this IServiceCollection services, RabbitMqConfig config)
    {
        IQueue QueueFactory(IServiceProvider serviceProvider)
        {
            return new RabbitMqQueue(
                host: config.Host, port: config.Port, user: config.Username, password: config.Password,
                log: serviceProvider.GetService<ILogger<RabbitMqQueue>>());
        }

        // The orchestrator uses multiple queue clients, each linked to a specific queue,
        // so it requires a factory rather than a single queue injected to the ctor.
        return services
            .AddSingleton<QueueClientFactory>(serviceProvider => new QueueClientFactory(() => QueueFactory(serviceProvider)));
    }
}
