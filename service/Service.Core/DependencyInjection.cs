// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.Configuration;
using Microsoft.OpenApi.Models;

namespace Microsoft.KernelMemory.Service.Core;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddKernelMemory(this WebApplicationBuilder appBuilder, Func<IKernelMemoryBuilder, IKernelMemoryBuilder> configure)
    {
        KernelMemoryConfig config = appBuilder.Configuration.GetSection("KernelMemory").Get<KernelMemoryConfig>()
             ?? throw new ConfigurationException("Unable to load configuration");
        config.ServiceAuthorization.Validate();

        // OpenAPI/swagger
        if (config.Service.RunWebService)
        {
            appBuilder.Services.AddEndpointsApiExplorer();
            appBuilder.Services.AddSwaggerGen(c =>
            {
                if (!config.ServiceAuthorization.Enabled) { return; }

                const string ReqName = "auth";
                c.AddSecurityDefinition(ReqName, new OpenApiSecurityScheme
                {
                    Description = "The API key to access the API",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "ApiKeyScheme",
                    Name = config.ServiceAuthorization.HttpHeaderName,
                    In = ParameterLocation.Header,
                });

                var scheme = new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Id = ReqName,
                        Type = ReferenceType.SecurityScheme,
                    },
                    In = ParameterLocation.Header
                };

                var requirement = new OpenApiSecurityRequirement
        {
            { scheme, new List<string>() }
        };

                c.AddSecurityRequirement(requirement);
            });
        }

        IServiceCollection services = appBuilder.Services;

        services.AddSingleton(config);

        IKernelMemoryBuilder builder = configure(new KernelMemoryBuilder(services));
        services.AddSingleton<IKernelMemory>(builder.Build());
        return appBuilder;
    }
}
