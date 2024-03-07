// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Microsoft.KernelMemory.Service;

internal static class OpenAPI
{
    public static void ConfigureSwagger(this WebApplicationBuilder appBuilder, KernelMemoryConfig config)
    {
        if (!config.Service.RunWebService || !config.Service.OpenApiEnabled) { return; }

        appBuilder.Services.AddEndpointsApiExplorer();

        // Note: this call is required even if service auth is disabled
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

    public static void UseSwagger(this WebApplication app, KernelMemoryConfig config)
    {
        if (!config.Service.RunWebService || !config.Service.OpenApiEnabled) { return; }

        // URL: http://localhost:9001/swagger/index.html
        app.UseSwagger();
        app.UseSwaggerUI();
    }
}
