// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Configuration;

namespace Microsoft.SemanticMemory.Diagnostics;

/// <summary>
/// Create and cache a logger instance using the same
/// configuration sources supported by Semantic Memory config.
/// </summary>
/// <typeparam name="T"></typeparam>
public static class DefaultLogger<T>
{
    public static readonly ILogger<T> Instance = GetLogFactory()
        .CreateLogger<T>();

    private static ILoggerFactory GetLogFactory()
    {
        return WebApplication.CreateBuilder().Build().Services.GetService<ILoggerFactory>()
               ?? throw new ConfigurationException("Unable to provide logger factory");
    }
}
