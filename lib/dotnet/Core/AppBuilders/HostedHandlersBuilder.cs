// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel.SemanticMemory.Core.Configuration;
using Microsoft.SemanticKernel.SemanticMemory.Core.Pipeline;

namespace Microsoft.SemanticKernel.SemanticMemory.Core.AppBuilders;

public static class HostedHandlersBuilder
{
    public static HostApplicationBuilder CreateApplicationBuilder(string[]? args = null)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToUpperInvariant() == "DEVELOPMENT")
        {
            builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true);
        }

        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToUpperInvariant() == "PRODUCTION")
        {
            builder.Configuration.AddJsonFile("appsettings.Production.json", optional: true);
        }

        SKMemoryConfig config = builder.Services.UseConfiguration(builder.Configuration);

        builder.Logging.ConfigureLogger();
        builder.Services.UseContentStorage(config);
        builder.Services.UseOrchestrator(config);

        return builder;
    }

    /// <summary>
    /// Register the handler as a hosted service, passing the step name to the handler ctor
    /// </summary>
    /// <param name="services">Application builder service collection</param>
    /// <param name="stepName">Pipeline step name</param>
    /// <typeparam name="THandler">Handler class</typeparam>
    public static void UseHandlerAsHostedService<THandler>(this IServiceCollection services, string stepName) where THandler : class, IPipelineStepHandler
    {
        services.UseHandler<THandler>(stepName);
        services.AddHostedService<HandlerAsAHostedService<THandler>>(serviceProvider
            => ActivatorUtilities.CreateInstance<HandlerAsAHostedService<THandler>>(serviceProvider, stepName));
    }

    // /// <summary>
    // /// Register the handler as a hosted service, passing the step name to the handler ctor
    // /// </summary>
    // /// <param name="builder">Application builder</param>
    // /// <param name="stepName">Pipeline step name</param>
    // /// <typeparam name="THandler">Handler class</typeparam>
    // public static void UseHandlerAsHostedService<THandler>(this HostApplicationBuilder builder, string stepName) where THandler : class, IPipelineStepHandler
    // {
    //     builder.Services.UseHandler<THandler>(stepName);
    //     builder.Services.AddHostedService<HandlerAsAHostedService<THandler>>(serviceProvider
    //         => ActivatorUtilities.CreateInstance<HandlerAsAHostedService<THandler>>(serviceProvider, stepName));
    // }
}
