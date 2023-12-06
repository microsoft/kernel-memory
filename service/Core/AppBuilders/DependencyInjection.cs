// Copyright (c) Microsoft. All rights reserved.

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.Pipeline;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

public static partial class DependencyInjection
{
    /// <summary>
    /// Register the handler as a hosted service, passing the step name to the handler ctor
    /// </summary>
    /// <param name="services">Application builder service collection</param>
    /// <param name="stepName">Pipeline step name</param>
    /// <typeparam name="THandler">Handler class</typeparam>
    public static void AddHandlerAsHostedService<THandler>(this IServiceCollection services, string stepName) where THandler : class, IPipelineStepHandler
    {
        services.AddTransient<THandler>(serviceProvider => ActivatorUtilities.CreateInstance<THandler>(serviceProvider, stepName));

        services.AddHostedService<HandlerAsAHostedService<THandler>>(
            serviceProvider => ActivatorUtilities.CreateInstance<HandlerAsAHostedService<THandler>>(serviceProvider, stepName));
    }

    /// <summary>
    /// Check if the service collection contains a descriptor for the given type
    /// </summary>
    /// <param name="services">Service Collection</param>
    /// <typeparam name="T">Type required</typeparam>
    /// <returns>True when the service collection contains T</returns>
    public static bool HasService<T>(this IServiceCollection services)
    {
        return (services.Any(x => x.ServiceType == typeof(T)));
    }
}
