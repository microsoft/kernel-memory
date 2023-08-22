// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.Pipeline.Queue;
using Microsoft.SemanticMemory.Pipeline.Queue.RabbitMq;

// ReSharper disable once CheckNamespace
namespace Microsoft.SemanticMemory;

public static partial class MemoryClientBuilderExtensions
{
    public static MemoryClientBuilder WithRabbitMQPipeline(this MemoryClientBuilder builder, RabbitMqConfig config)
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
                   ?? throw new SemanticMemoryException("Unable to instantiate " + typeof(RabbitMqQueue));
        }

        // The orchestrator uses multiple queue clients, each linked to a specific queue,
        // so it requires a factory rather than a single queue injected to the ctor.
        return services
            .AddSingleton<RabbitMqConfig>(config)
            .AddTransient<RabbitMqQueue>()
            .AddSingleton<QueueClientFactory>(serviceProvider => new QueueClientFactory(() => QueueFactory(serviceProvider)));
    }
}
