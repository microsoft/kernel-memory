// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.Handlers;
using Microsoft.KernelMemory.Pipeline;

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
    /// Register the handler as a hosted service, passing the step name to the handler ctor
    /// </summary>
    /// <param name="services">Application builder service collection</param>
    /// <param name="tHandler">Handler class</param>
    /// <param name="stepName">Pipeline step name</param>
    public static void AddHandlerAsHostedService(this IServiceCollection services, Type tHandler, string stepName)
    {
        if (!typeof(IPipelineStepHandler).IsAssignableFrom(tHandler))
        {
            throw new ArgumentException($"'{tHandler.FullName}' doesn't implement interface '{nameof(IPipelineStepHandler)}'", nameof(tHandler));
        }

        if (tHandler == null)
        {
            throw new ArgumentNullException(nameof(tHandler), $"Handler type for '{stepName}' is NULL");
        }

        services.AddTransient(tHandler, serviceProvider => ActivatorUtilities.CreateInstance(serviceProvider, tHandler, stepName));

        // Build generic type: HandlerAsAHostedService<THandler>
        Type handlerAsAHostedServiceTHandler = typeof(HandlerAsAHostedService<>).MakeGenericType(tHandler);

        Func<IServiceProvider, IHostedService> implementationFactory =
            serviceProvider => (IHostedService)ActivatorUtilities.CreateInstance(serviceProvider, handlerAsAHostedServiceTHandler, stepName);

        // See https://github.com/dotnet/runtime/issues/38751 for troubleshooting
        services.Add(ServiceDescriptor.Singleton<IHostedService>(implementationFactory));
    }

    /// <summary>
    /// Register the handler as a hosted service, passing the step name to the handler ctor
    /// </summary>
    /// <param name="services">Application builder service collection</param>
    /// <param name="config">Handler type configuration</param>
    /// <param name="stepName">Pipeline step name</param>
    public static void AddHandlerAsHostedService(this IServiceCollection services, HandlerConfig config, string stepName)
    {
        if (HandlerTypeLoader.TryGetHandlerType(config, out var handlerType))
        {
            services.AddHandlerAsHostedService(handlerType, stepName);
        }
    }

    /// <summary>
    /// Register the handler as a hosted service, passing the step name to the handler ctor
    /// </summary>
    /// <param name="services">Application builder service collection</param>
    /// <param name="assemblyFile">Path to assembly containing handler class</param>
    /// <param name="typeFullName">Handler type, within the assembly</param>
    /// <param name="stepName">Pipeline step name</param>
    public static void AddHandlerAsHostedService(this IServiceCollection services, string assemblyFile, string typeFullName, string stepName)
    {
        services.AddHandlerAsHostedService(new HandlerConfig(assemblyFile, typeFullName), stepName);
    }
}
