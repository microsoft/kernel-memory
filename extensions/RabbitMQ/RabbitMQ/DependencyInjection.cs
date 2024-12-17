// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.Orchestration.RabbitMQ;
using Microsoft.KernelMemory.Pipeline.Queue;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithRabbitMQOrchestration(this IKernelMemoryBuilder builder, RabbitMQConfig config)
    {
        builder.Services.AddRabbitMQOrchestration(config);
        return builder;
    }
}

/// <summary>
/// .NET IServiceCollection dependency injection extensions.
/// </summary>
public static partial class DependencyInjection
{
    public static IServiceCollection AddRabbitMQOrchestration(this IServiceCollection services, RabbitMQConfig config)
    {
        config.Validate();

        static IQueue QueueFactory(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<RabbitMQPipeline>()
                   ?? throw new KernelMemoryException("Unable to instantiate " + typeof(RabbitMQPipeline));
        }

        // The orchestrator uses multiple queue clients, each linked to a specific queue,
        // so it requires a factory rather than a single queue injected to the ctor.
        return services
            .AddSingleton<RabbitMQConfig>(config)
            .AddTransient<RabbitMQPipeline>()
            .AddSingleton<QueueClientFactory>(serviceProvider => new QueueClientFactory(() => QueueFactory(serviceProvider)));
    }
}
