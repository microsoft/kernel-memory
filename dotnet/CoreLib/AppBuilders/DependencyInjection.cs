// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.Pipeline;
using Microsoft.SemanticMemory.Core.Search;

namespace Microsoft.SemanticMemory.Core.AppBuilders;

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
        // services.UseHandler<THandler>(stepName);
        services.AddTransient<THandler>(serviceProvider => ActivatorUtilities.CreateInstance<THandler>(serviceProvider, stepName));

        services.AddHostedService<HandlerAsAHostedService<THandler>>(serviceProvider
            => ActivatorUtilities.CreateInstance<HandlerAsAHostedService<THandler>>(serviceProvider, stepName));
    }

    /// <summary>
    /// Configure the internal dependencies using settings from the configuration.
    /// Storage, Vector DB, Embedding and Text generation are configured on the caller side to allow injecting custom services. 
    /// </summary>
    public static void ConfigureRuntime(this IServiceCollection services, SemanticMemoryConfig config)
    {
        services.AddSingleton<SemanticMemoryConfig>(config);
        services.AddSingleton<SearchClient, SearchClient>();
        services.AddSingleton<IMimeTypeDetection, MimeTypesDetection>();

        if (config.Service.RunWebService)
        {
            services.AddSingleton<ISemanticMemoryService, SemanticMemoryService>();

            if (config.Service.OpenApiEnabled)
            {
                services.AddEndpointsApiExplorer();
                services.AddSwaggerGen();
            }
        }

        switch (config.DataIngestion.OrchestrationType)
        {
            case "Distributed":
                services.AddSingleton<IPipelineOrchestrator, DistributedPipelineOrchestrator>();
                services.AddSingleton<DistributedPipelineOrchestrator, DistributedPipelineOrchestrator>();
                break;

            case "InProcess":
                services.AddSingleton<IPipelineOrchestrator, InProcessPipelineOrchestrator>();
                services.AddSingleton<InProcessPipelineOrchestrator, InProcessPipelineOrchestrator>();
                break;

            default:
                throw new NotSupportedException($"Unknown/unsupported {config.DataIngestion.OrchestrationType} orchestration");
        }
    }
}
