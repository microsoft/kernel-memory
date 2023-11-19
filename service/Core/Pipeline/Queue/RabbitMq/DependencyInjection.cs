// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.Pipeline.Queue;
using Microsoft.KernelMemory.Pipeline.Queue.RabbitMq;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    public static KernelMemoryBuilder WithRabbitMQPipeline(this KernelMemoryBuilder builder, RabbitMqConfig config)
    {
        builder.Services.AddRabbitMq(config);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddRabbitMq(this IServiceCollection services, RabbitMqConfig config)
    {
        IQueue QueueFactory(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<RabbitMqQueue>()
                   ?? throw new KernelMemoryException("Unable to instantiate " + typeof(RabbitMqQueue));
        }

        // The orchestrator uses multiple queue clients, each linked to a specific queue,
        // so it requires a factory rather than a single queue injected to the ctor.
        return services
            .AddSingleton<RabbitMqConfig>(config)
            .AddTransient<RabbitMqQueue>()
            .AddSingleton<QueueClientFactory>(serviceProvider => new QueueClientFactory(() => QueueFactory(serviceProvider)));
    }
}
