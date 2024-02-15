// Copyright (c) Microsoft. All rights reserved.

using System;
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

internal sealed class Program
{
    public static void Main(string[] args)
    {
        // *************************** CONFIG WIZARD ***************************

        // Run `dotnet run setup` to run this code and setup the service
        if (new[] { "setup", "-setup", "config" }.Contains(args.FirstOrDefault(), StringComparer.OrdinalIgnoreCase))
        {
            InteractiveSetup.Main.InteractiveSetup(args.Skip(1).ToArray(), cfgService: true);
        }

        // *************************** APP BUILD *******************************

        // Usual .NET web app builder
        WebApplicationBuilder appBuilder = WebApplication.CreateBuilder();
        appBuilder.Configuration.AddKMConfigurationSources();

        // Read settings and validate. Settings are needed before building the app.
        KernelMemoryConfig config = appBuilder.Configuration.GetSection("KernelMemory").Get<KernelMemoryConfig>()
                                    ?? throw new ConfigurationException("Unable to load configuration");
        config.ServiceAuthorization.Validate();

        // Some OpenAPI Explorer/Swagger dependencies
        appBuilder.ConfigureSwagger(config);

        // Inject memory client and its dependencies
        // Note: pass the current service collection to the builder, in order to start the pipeline handlers
        IKernelMemory memory = new KernelMemoryBuilder(appBuilder.Services)
            .FromAppSettings()
            // .With...() // in case you need to set something not already defined by `.FromAppSettings()`
            .Build();

        appBuilder.Services.AddSingleton(memory);

        // Build .NET web app as usual, add HTTP endpoints using minimal API (https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)
        WebApplication app = appBuilder.Build();
        app.ConfigureMinimalAPI(config);

        // *************************** START ***********************************

        Console.WriteLine("***************************************************************************************************************************");
        Console.WriteLine($"* Environment         : " + Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "WARNING: ASPNETCORE_ENVIRONMENT env var not defined");
        Console.WriteLine($"* Web service         : " + (config.Service.RunWebService ? "Enabled" : "Disabled"));
        Console.WriteLine($"* Web service auth    : " + (config.ServiceAuthorization.Enabled ? "Enabled" : "Disabled"));
        Console.WriteLine($"* Pipeline handlers   : " + (config.Service.RunHandlers ? "Enabled" : "Disabled"));
        Console.WriteLine($"* OpenAPI swagger     : " + (config.Service.OpenApiEnabled ? "Enabled" : "Disabled"));
        Console.WriteLine($"* Logging level       : {app.Logger.GetLogLevelName()}");
        Console.WriteLine($"* Memory Db           : {app.Services.GetService<IMemoryDb>()?.GetType().FullName}");
        Console.WriteLine($"* Content storage     : {app.Services.GetService<IContentStorage>()?.GetType().FullName}");
        Console.WriteLine($"* Embedding generation: {app.Services.GetService<ITextEmbeddingGenerator>()?.GetType().FullName}");
        Console.WriteLine($"* Text generation     : {app.Services.GetService<ITextGenerator>()?.GetType().FullName}");
        Console.WriteLine("***************************************************************************************************************************");

        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty;
        app.Logger.LogInformation(
            "Starting Kernel Memory service, .NET Env: {0}, Log Level: {1}, Web service: {2}, Auth: {3}, Pipeline handlers: {4}",
            env,
            app.Logger.GetLogLevelName(),
            config.Service.RunWebService,
            config.ServiceAuthorization.Enabled,
            config.Service.RunHandlers);

        if (string.IsNullOrEmpty(env))
        {
            app.Logger.LogError("ASPNETCORE_ENVIRONMENT env var not defined.");
        }

        app.Run();
    }
}
