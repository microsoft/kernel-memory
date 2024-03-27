// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.ContentStorage;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.Pipeline;

// KM Configuration:
//
// * Settings are loaded at runtime from multiple sources, merging values.
//   Each configuration source can override settings from the previous source:
//   - appsettings.json              (default values)
//   - appsettings.Production.json   (only if ASPNETCORE_ENVIRONMENT == "Production", e.g. in the Docker image)
//   - appsettings.Development.json  (only if ASPNETCORE_ENVIRONMENT == "Development")
//   - .NET Secret Manager           (only if ASPNETCORE_ENVIRONMENT == "Development" - see https://learn.microsoft.com/aspnet/core/security/app-secrets#secret-manager)
//   - environment variables         (these can override everything else from the previous sources)
//
// * You should set ASPNETCORE_ENVIRONMENT env var if you want to use also appsettings.<env>.json
//   * In production environments:
//          Set ASPNETCORE_ENVIRONMENT = Production
//          and the app will try to load appsettings.Production.json
//
//   * In local dev workstations:
//          Set ASPNETCORE_ENVIRONMENT = Development
//          and the app will try to load appsettings.Development.json
//          In dev mode the app will also look for settings in .NET Secret Manager
//
// * The app supports also environment variables, e.g.
//   to set: KernelMemory.Service.RunWebService = true
//   use an env var:   KernelMemory__Service__RunWebService = true

namespace Microsoft.KernelMemory.Service;

internal static class Program
{
    public static void Main(string[] args)
    {
        // *************************** CONFIG WIZARD ***************************

        // Run `dotnet run setup` to run this code and setup the service
        if (new[] { "setup", "-setup", "config" }.Contains(args.FirstOrDefault(), StringComparer.OrdinalIgnoreCase))
        {
            InteractiveSetup.Main.InteractiveSetup(args.Skip(1).ToArray());
        }

        // *************************** APP BUILD *******************************

        // Usual .NET web app builder with settings from appsettings.json, appsettings.<ENV>.json, and env vars
        WebApplicationBuilder appBuilder = WebApplication.CreateBuilder();
        appBuilder.Configuration.AddKMConfigurationSources();

        // Read KM settings, needed before building the app.
        KernelMemoryConfig config = appBuilder.Configuration.GetSection("KernelMemory").Get<KernelMemoryConfig>()
                                    ?? throw new ConfigurationException("Unable to load configuration");

        // Some OpenAPI Explorer/Swagger dependencies
        appBuilder.ConfigureSwagger(config);

        // Prepare memory builder, sharing the service collection used by the hosting service
        var memoryBuilder = new KernelMemoryBuilder(appBuilder.Services).WithoutDefaultHandlers();

        // When using distributed orchestration, handlers are hosted in the current app
        var asyncHandlersCount = AddHandlersToHostingApp(config, memoryBuilder, appBuilder);

        // Build the memory client and make it available for dependency injection
        var memory = memoryBuilder.FromAppSettings().Build();
        appBuilder.Services.AddSingleton<IKernelMemory>(memory);

        // When using in process orchestration, handlers are hosted by the memory orchestrator
        var syncHandlersCount = AddHandlersToOrchestrator(config, memory);

        // Build .NET web app as usual
        WebApplication app = appBuilder.Build();

        // Add HTTP endpoints using minimal API (https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)
        app.ConfigureMinimalAPI(config);

        // *************************** START ***********************************

        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (string.IsNullOrEmpty(env))
        {
            app.Logger.LogError("ASPNETCORE_ENVIRONMENT env var not defined.");
        }

        Console.WriteLine("***************************************************************************************************************************");
        Console.WriteLine("* Environment         : " + (string.IsNullOrEmpty(env) ? "WARNING: ASPNETCORE_ENVIRONMENT env var not defined" : env));
        Console.WriteLine("* Memory type         : " + ((memory is MemoryServerless) ? "Sync - " : "Async - ") + memory.GetType().FullName);
        Console.WriteLine("* Pipeline handlers   : " + $"{syncHandlersCount} synchronous / {asyncHandlersCount} asynchronous");
        Console.WriteLine("* Web service         : " + (config.Service.RunWebService ? "Enabled" : "Disabled"));
        Console.WriteLine("* Web service auth    : " + (config.ServiceAuthorization.Enabled ? "Enabled" : "Disabled"));
        Console.WriteLine("* OpenAPI swagger     : " + (config.Service.OpenApiEnabled ? "Enabled" : "Disabled"));
        Console.WriteLine("* Memory Db           : " + app.Services.GetService<IMemoryDb>()?.GetType().FullName);
        Console.WriteLine("* Content storage     : " + app.Services.GetService<IContentStorage>()?.GetType().FullName);
        Console.WriteLine("* Embedding generation: " + app.Services.GetService<ITextEmbeddingGenerator>()?.GetType().FullName);
        Console.WriteLine("* Text generation     : " + app.Services.GetService<ITextGenerator>()?.GetType().FullName);
        Console.WriteLine("* Log level           : " + app.Logger.GetLogLevelName());
        Console.WriteLine("***************************************************************************************************************************");

        app.Logger.LogInformation(
            "Starting Kernel Memory service, .NET Env: {0}, Log Level: {1}, Web service: {2}, Auth: {3}, Pipeline handlers: {4}",
            env,
            app.Logger.GetLogLevelName(),
            config.Service.RunWebService,
            config.ServiceAuthorization.Enabled,
            config.Service.RunHandlers);

        // Start web service and handler services
        app.Run();
    }

    /// <summary>
    /// Register handlers as asynchronous hosted services
    /// </summary>
    private static int AddHandlersToHostingApp(
        KernelMemoryConfig config,
        IKernelMemoryBuilder memoryBuilder,
        WebApplicationBuilder appBuilder)
    {
        if (config.DataIngestion.OrchestrationType != "Distributed") { return 0; }

        if (!config.Service.RunHandlers) { return 0; }

        // Handlers are enabled via configuration in appsettings.json and/or appsettings.<env>.json
        memoryBuilder.WithoutDefaultHandlers();

        // You can add handlers in the configuration or manually here using one of these syntaxes:
        // appBuilder.Services.AddHandlerAsHostedService<...CLASS...>("...STEP NAME...");
        // appBuilder.Services.AddHandlerAsHostedService("...assembly file name...", "...type full name...", "...STEP NAME...");

        // Register all pipeline handlers defined in the configuration to run as hosted services
        foreach (KeyValuePair<string, HandlerConfig> handlerConfig in config.Service.Handlers)
        {
            appBuilder.Services.AddHandlerAsHostedService(config: handlerConfig.Value, stepName: handlerConfig.Key);
        }

        // Return registered handlers count
        return appBuilder.Services.Count(s => typeof(IPipelineStepHandler).IsAssignableFrom(s.ServiceType));
    }

    /// <summary>
    /// Register handlers instances inside the synchronous orchestrator
    /// </summary>
    private static int AddHandlersToOrchestrator(
        KernelMemoryConfig config, IKernelMemory memory)
    {
        if (memory is not MemoryServerless) { return 0; }

        var orchestrator = ((MemoryServerless)memory).Orchestrator;
        foreach (KeyValuePair<string, HandlerConfig> handlerConfig in config.Service.Handlers)
        {
            orchestrator.AddSynchronousHandler(handlerConfig.Value, handlerConfig.Key);
        }

        return orchestrator.HandlerNames.Count;
    }
}
