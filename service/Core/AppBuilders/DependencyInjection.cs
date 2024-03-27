﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    /// <param name="stepName">Pipeline step name</param>
    /// <typeparam name="THandler">Handler class</typeparam>
    public static void AddHandlerAsHostedService(this IServiceCollection services, Type pipelineStepHandler, string stepName)
    {
        services.AddTransient(serviceProvider => ActivatorUtilities.CreateInstance(serviceProvider, pipelineStepHandler, stepName));
        services.AddHostedService(serviceProvider => (IHostedService)ActivatorUtilities.CreateInstance(serviceProvider, pipelineStepHandler, stepName));
    }
}