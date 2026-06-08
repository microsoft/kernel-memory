// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.DocumentStorage;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.KernelMemory.Service.AspNetCore;

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
    private static readonly DateTimeOffset s_start = DateTimeOffset.UtcNow;

    public static void Main(string[] args)
    {
        SensitiveDataLogger.Enabled = false;

        // *************************** CONFIG WIZARD ***************************

        // Run `dotnet run setup` to run this code and set up the service
        if (new[] { "setup", "--setup", "config" }.Contains(args.FirstOrDefault(), StringComparer.OrdinalIgnoreCase))
        {
            InteractiveSetup.Program.Main(args.Skip(1).ToArray());
        }

        // Run `dotnet run check` to run this code and analyze the service configuration
        if (new[] { "check", "--check" }.Contains(args.FirstOrDefault(), StringComparer.OrdinalIgnoreCase))
        {
            InteractiveSetup.Program.Main(["--check"]);
        }

        // *************************** APP BUILD *******************************

        int asyncHandlersCount = 0;
        int syncHandlersCount = 0;
        string memoryType = string.Empty;

        // Usual .NET web app builder with settings from appsettings.json, appsettings.<ENV>.json, and env vars
        WebApplicationBuilder appBuilder = WebApplication.CreateBuilder();

        if (Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING") != null)
        {
            appBuilder.Services.AddApplicationInsightsTelemetry();
        }

        // Add config files, user secretes, and env vars
        appBuilder.Configuration.AddKernelMemoryConfigurationSources();

        // Read KM settings, needed before building the app.
        KernelMemoryConfig config = appBuilder.Configuration.GetSection("KernelMemory").Get<KernelMemoryConfig>()
                                    ?? throw new ConfigurationException("Unable to load configuration");

        // Some OpenAPI Explorer/Swagger dependencies
        appBuilder.ConfigureSwagger(config);

        // Prepare memory builder, sharing the service collection used by the hosting service
        // Internally build the memory client and make it available for dependency injection
        appBuilder.AddKernelMemory(memoryBuilder =>
            {
                // Prepare the builder with settings from config files
                memoryBuilder.ConfigureDependencies(appBuilder.Configuration).WithoutDefaultHandlers();

                // When using distributed orchestration, handlers are hosted in the current app and need to be con
                asyncHandlersCount = AddHandlersAsHostedServices(config, memoryBuilder, appBuilder);
            },
            memory =>
            {
                // When using in process orchestration, handlers are hosted by the memory orchestrator
                syncHandlersCount = AddHandlersToServerlessMemory(config, memory);

                memoryType = ((memory is MemoryServerless) ? "Sync - " : "Async - ") + memory.GetType().FullName;
            },
            services =>
            {
                long? maxSize = config.Service.GetMaxUploadSizeInBytes();
                if (!maxSize.HasValue) { return; }

                services.Configure<IISServerOptions>(x => { x.MaxRequestBodySize = maxSize.Value; });
                services.Configure<KestrelServerOptions>(x => { x.Limits.MaxRequestBodySize = maxSize.Value; });
                services.Configure<FormOptions>(x =>
                {
                    x.MultipartBodyLengthLimit = maxSize.Value;
                    x.ValueLengthLimit = int.MaxValue;
                });
            });

        // CORS
        bool enableCORS = false;
        const string CORSPolicyName = "KM-CORS";
        if (enableCORS && config.Service.RunWebService)
        {
            appBuilder.Services.AddCors(options =>
            {
                options.AddPolicy(name: CORSPolicyName, policy =>
                {
                    policy
                        .WithMethods("HEAD", "GET", "POST", "PUT", "DELETE")
                        .WithExposedHeaders("Content-Type", "Content-Length", "Last-Modified");
                    // .AllowAnyOrigin()
                    // .WithOrigins(...)
                    // .AllowAnyHeader()
                    // .WithHeaders(...)
                });
            });
        }

        // Build .NET web app as usual
        WebApplication app = appBuilder.Build();

        if (config.Service.RunWebService)
        {
            if (enableCORS) { app.UseCors(CORSPolicyName); }

            app.UseSwagger(config);
            var errorFilter = new HttpErrorsEndpointFilter();
            var authFilter = new HttpAuthEndpointFilter(config.ServiceAuthorization);
            app.MapGet("/", () => Results.Ok("Ingestion service is running. " +
                                             "Uptime: " + (DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                                                           - s_start.ToUnixTimeSeconds()) + " secs " +
                                             $"- Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}"))
                .AddEndpointFilter(errorFilter)
                .AddEndpointFilter(authFilter)
                .WithName("ServiceStatus")
                .WithDisplayName("ServiceStatus")
                .WithDescription("Show the service status and uptime.")
                .WithSummary("Show the service status and uptime.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

            // Add HTTP endpoints using minimal API (https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)
            app.AddKernelMemoryEndpoints("/", config, [errorFilter, authFilter]);

            // Health probe
            app.MapGet("/health", () => Results.Ok("Service is running."))
                .WithName("ServiceHealth")
                .WithDisplayName("ServiceHealth")
                .WithDescription("Show if the service is healthy.")
                .WithSummary("Show if the service is healthy.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

            if (config.ServiceAuthorization.Enabled && config.ServiceAuthorization.AccessKey1 == config.ServiceAuthorization.AccessKey2)
            {
                app.Logger.LogError("KM Web Service: Access keys 1 and 2 have the same value. Keys should be different to allow rotation.");
            }
        }

        // *************************** START ***********************************

        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (string.IsNullOrEmpty(env))
        {
            app.Logger.LogError("ASPNETCORE_ENVIRONMENT env var not defined.");
        }

        Console.WriteLine("***************************************************************************************************************************");
        Console.WriteLine("* Environment         : " + (string.IsNullOrEmpty(env) ? "WARNING: ASPNETCORE_ENVIRONMENT env var not defined" : env));
        Console.WriteLine("* Memory type         : " + memoryType);
        Console.WriteLine("* Pipeline handlers   : " + $"{syncHandlersCount} synchronous / {asyncHandlersCount} asynchronous");
        Console.WriteLine("* Web service         : " + (config.Service.RunWebService ? "Enabled" : "Disabled"));

        if (config.Service.RunWebService)
        {
            const double AspnetDefaultMaxUploadSize = 30000000d / 1024 / 1024;
            Console.WriteLine("* Web service auth    : " + (config.ServiceAuthorization.Enabled ? "Enabled" : "Disabled"));
            Console.WriteLine("* Max HTTP req size   : " + (config.Service.MaxUploadSizeMb ?? AspnetDefaultMaxUploadSize).ToString("0.#", CultureInfo.CurrentCulture) + " Mb");
            Console.WriteLine("* OpenAPI swagger     : " + (config.Service.OpenApiEnabled ? "Enabled (/swagger/index.html)" : "Disabled"));
        }

        Console.WriteLine("* Memory Db           : " + app.Services.GetService<IMemoryDb>()?.GetType().FullName);
        Console.WriteLine("* Document storage    : " + app.Services.GetService<IDocumentStorage>()?.GetType().FullName);
        Console.WriteLine("* Embedding generation: " + app.Services.GetService<ITextEmbeddingGenerator>()?.GetType().FullName);
        Console.WriteLine("* Text generation     : " + app.Services.GetService<ITextGenerator>()?.GetType().FullName);
        Console.WriteLine("* Content moderation  : " + app.Services.GetService<IContentModeration>()?.GetType().FullName);
        Console.WriteLine("* Log level           : " + app.Logger.GetLogLevelName());
        Console.WriteLine("***************************************************************************************************************************");

        app.Logger.LogInformation(
            "Starting Kernel Memory service, .NET Env: {EnvironmentType}, Log Level: {LogLevel}, Web service: {WebServiceEnabled}, Auth: {WebServiceAuthEnabled}, Pipeline handlers: {HandlersEnabled}",
            env,
            app.Logger.GetLogLevelName(),
            config.Service.RunWebService,
            config.ServiceAuthorization.Enabled,
            config.Service.RunHandlers);

        // Start web service and handler services
        try
        {
            app.Run();
        }
        catch (IOException e)
        {
            Console.WriteLine($"I/O error: {e.Message}");
            Environment.Exit(-1);
        }
    }

    /// <summary>
    /// Register handlers as asynchronous hosted services
    /// </summary>
    private static int AddHandlersAsHostedServices(
        KernelMemoryConfig config,
        IKernelMemoryBuilder memoryBuilder,
        WebApplicationBuilder appBuilder)
    {
        if (!string.Equals(config.DataIngestion.OrchestrationType, KernelMemoryConfig.OrchestrationTypeDistributed, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

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
    private static int AddHandlersToServerlessMemory(
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
