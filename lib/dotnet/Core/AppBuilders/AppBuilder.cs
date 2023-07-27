// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticMemory.Core.Configuration;

namespace Microsoft.SemanticMemory.Core.AppBuilders;

public static class AppBuilder
{
    public static IHost Build(Action<HostApplicationBuilder>? builderSetup = null, string[]? args = null)
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

        builderSetup?.Invoke(builder);

        return builder.Build();
    }
}
