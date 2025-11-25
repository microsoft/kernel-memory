// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.Handlers;
using Microsoft.KernelMemory.Pipeline;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    /// <summary>
    /// Register default handlers in the service collection used by the app hosting the asynchronous memory service
    /// </summary>
    public static IKernelMemoryBuilder WithDefaultHandlersAsHostedServices(this IKernelMemoryBuilder builder, IServiceCollection hostServices)
    {
        hostServices.AddDefaultHandlersAsHostedServices();
        return builder;
    }
}

/// <summary>
/// .NET IServiceCollection dependency injection extensions.
/// </summary>
public static partial class DependencyInjection
{
    /// <summary>
    /// Register default handlers in the synchronous orchestrator (e.g. when not using queues)
    /// </summary>
    /// <param name="syncOrchestrator">Instance of <see cref="InProcessPipelineOrchestrator"/></param>
    public static InProcessPipelineOrchestrator AddDefaultHandlers(this InProcessPipelineOrchestrator syncOrchestrator)
    {
        syncOrchestrator.AddHandler<TextExtractionHandler>(Constants.PipelineStepsExtract);
        syncOrchestrator.AddHandler<TextPartitioningHandler>(Constants.PipelineStepsPartition);
        syncOrchestrator.AddHandler<SummarizationHandler>(Constants.PipelineStepsSummarize);
        syncOrchestrator.AddHandler<GenerateEmbeddingsHandler>(Constants.PipelineStepsGenEmbeddings);
        syncOrchestrator.AddHandler<SaveRecordsHandler>(Constants.PipelineStepsSaveRecords);
        syncOrchestrator.AddHandler<DeleteDocumentHandler>(Constants.PipelineStepsDeleteDocument);
        syncOrchestrator.AddHandler<DeleteIndexHandler>(Constants.PipelineStepsDeleteIndex);
        syncOrchestrator.AddHandler<DeleteGeneratedFilesHandler>(Constants.PipelineStepsDeleteGeneratedFiles);

        // Experimental handlers using parallelism
        syncOrchestrator.AddHandler<GenerateEmbeddingsParallelHandler>("gen_embeddings_parallel");
        syncOrchestrator.AddHandler<SummarizationParallelHandler>("summarize_parallel");

        return syncOrchestrator;
    }

    /// <summary>
    /// Register default handlers in the service collection used by the app hosting the asynchronous memory service
    /// </summary>
    /// <param name="services">Host application service collection</param>
    public static IServiceCollection AddDefaultHandlersAsHostedServices(this IServiceCollection services)
    {
        services.AddHandlerAsHostedService<TextExtractionHandler>(Constants.PipelineStepsExtract);
        services.AddHandlerAsHostedService<TextPartitioningHandler>(Constants.PipelineStepsPartition);
        services.AddHandlerAsHostedService<SummarizationHandler>(Constants.PipelineStepsSummarize);
        services.AddHandlerAsHostedService<GenerateEmbeddingsHandler>(Constants.PipelineStepsGenEmbeddings);
        services.AddHandlerAsHostedService<SaveRecordsHandler>(Constants.PipelineStepsSaveRecords);
        services.AddHandlerAsHostedService<DeleteDocumentHandler>(Constants.PipelineStepsDeleteDocument);
        services.AddHandlerAsHostedService<DeleteIndexHandler>(Constants.PipelineStepsDeleteIndex);
        services.AddHandlerAsHostedService<DeleteGeneratedFilesHandler>(Constants.PipelineStepsDeleteGeneratedFiles);

        // Experimental handlers using parallelism
        services.AddHandlerAsHostedService<GenerateEmbeddingsParallelHandler>("gen_embeddings_parallel");
        services.AddHandlerAsHostedService<SummarizationParallelHandler>("summarize_parallel");

        return services;
    }

    /// <summary>
    /// Register the handler as a hosted service, passing the step name to the handler ctor
    /// </summary>
    /// <param name="services">Application builder service collection</param>
    /// <param name="stepName">Pipeline step name</param>
    /// <typeparam name="THandler">Handler class</typeparam>
    public static IServiceCollection AddHandlerAsHostedService<THandler>(
        this IServiceCollection services, string stepName) where THandler : class, IPipelineStepHandler
    {
        services.AddTransient<THandler>(
            serviceProvider => ActivatorUtilities.CreateInstance<THandler>(serviceProvider, stepName));

        services.AddHostedService<HandlerAsAHostedService<THandler>>(
            serviceProvider => ActivatorUtilities.CreateInstance<HandlerAsAHostedService<THandler>>(serviceProvider, stepName));

        return services;
    }

    /// <summary>
    /// Register the handler as a hosted service, passing the step name to the handler ctor
    /// </summary>
    /// <param name="services">Application builder service collection</param>
    /// <param name="tHandler">Handler class</param>
    /// <param name="stepName">Pipeline step name</param>
    public static IServiceCollection AddHandlerAsHostedService(
        this IServiceCollection services, Type tHandler, string stepName)
    {
        if (!typeof(IPipelineStepHandler).IsAssignableFrom(tHandler))
        {
            throw new ArgumentException($"'{tHandler.FullName}' doesn't implement interface '{nameof(IPipelineStepHandler)}'", nameof(tHandler));
        }

        ArgumentNullExceptionEx.ThrowIfNull(tHandler, nameof(tHandler), $"Handler type for '{stepName}' is NULL");
        services.AddTransient(tHandler, serviceProvider => ActivatorUtilities.CreateInstance(serviceProvider, tHandler, stepName));

        // Build generic type: HandlerAsAHostedService<THandler>
        Type handlerAsAHostedServiceTHandler = typeof(HandlerAsAHostedService<>).MakeGenericType(tHandler);

        IHostedService ImplementationFactory(IServiceProvider serviceProvider)
            => (IHostedService)ActivatorUtilities.CreateInstance(serviceProvider, handlerAsAHostedServiceTHandler, stepName);

        // See https://github.com/dotnet/runtime/issues/38751 for troubleshooting
        services.Add(ServiceDescriptor.Singleton<IHostedService>((Func<IServiceProvider, IHostedService>)ImplementationFactory));

        return services;
    }

    /// <summary>
    /// Register the handler as a hosted service, passing the step name to the handler ctor
    /// </summary>
    /// <param name="services">Application builder service collection</param>
    /// <param name="config">Handler type configuration</param>
    /// <param name="stepName">Pipeline step name</param>
    public static IServiceCollection AddHandlerAsHostedService(
        this IServiceCollection services, HandlerConfig config, string stepName)
    {
        if (HandlerTypeLoader.TryGetHandlerType(config, out var handlerType))
        {
            services.AddHandlerAsHostedService(handlerType, stepName);
        }

        return services;
    }

    /// <summary>
    /// Register the handler as a hosted service, passing the step name to the handler ctor
    /// </summary>
    /// <param name="services">Application builder service collection</param>
    /// <param name="assemblyFile">Path to assembly containing handler class</param>
    /// <param name="typeFullName">Handler type, within the assembly</param>
    /// <param name="stepName">Pipeline step name</param>
    public static IServiceCollection AddHandlerAsHostedService(
        this IServiceCollection services, string assemblyFile, string typeFullName, string stepName)
    {
        services.AddHandlerAsHostedService(new HandlerConfig(assemblyFile, typeFullName), stepName);

        return services;
    }
}
