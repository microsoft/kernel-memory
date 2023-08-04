// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.Core.Configuration;

namespace Microsoft.SemanticMemory.Core.AppBuilders;

public static class AppBuilder
{
    public static WebApplication Build(Action<IServiceCollection, SemanticMemoryConfig>? servicesConfiguration = null)
    {
        // Note: WebApplicationBuilder automatically handles appsettings.*.json
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Config first
        SemanticMemoryConfig config = builder.Services.UseConfiguration(builder.Configuration);

        // Logger second
        builder.Logging.ConfigureLogger();

        // Other dependencies, used everywhere
        builder.Services.UseContentStorage();
        builder.Services.UseOrchestrator();

        // Optional settings from the caller
        servicesConfiguration?.Invoke(builder.Services, config);

        WebApplication app = builder.Build();
        return app;
    }
}
