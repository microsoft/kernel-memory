// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.Core.AppBuilders;

namespace Microsoft.SemanticMemory.Core.Configuration;

public class SemanticMemoryConfig
{
    /// <summary>
    /// Semantic Memory Service settings.
    /// </summary>
    public ServiceConfig Service { get; set; } = new ServiceConfig();

    /// <summary>
    /// Content storage settings, e.g. Azure Blob or File System details
    /// </summary>
    public ContentStorageConfig ContentStorage { get; set; } = new();

    /// <summary>
    /// Memory ingestion pipeline settings, including queueing system
    /// </summary>
    public OrchestrationConfig Orchestration { get; set; } = new();

    /// <summary>
    /// Memory ingestion pipeline handlers settings, e.g. settings about chunking, insights, and embeddings.
    /// </summary>
    public Dictionary<string, IConfigurationSection> Handlers { get; set; } = new();

    /// <summary>
    /// Web service settings, e.g. whether to expose OpenAPI swagger docs.
    /// </summary>
    public bool OpenApiEnabled { get; set; } = false;

    /// <summary>
    /// Search settings
    /// </summary>
    public SearchConfig Search { get; set; } = new();

    /// <summary>
    /// Get pipeline handler configuration.
    /// </summary>
    /// <param name="handlerName">Handler name</param>
    /// <typeparam name="T">Type of handler configuration</typeparam>
    /// <returns>Configuration data, mapped by .NET configuration binder</returns>
    public T GetHandlerConfig<T>(string handlerName) where T : class, new()
    {
        if (!this.Handlers.TryGetValue(handlerName, out IConfigurationSection? section))
        {
            return new T();
        }

        return section.Get<T>() ?? new T();
    }

    public static SemanticMemoryConfig LoadFromAppSettings()
    {
        var config = AppBuilder.Build().Services.GetService<SemanticMemoryConfig>();
        if (config == null)
        {
            throw new ConfigurationException("Configuration settings are empty");
        }

        return config;
    }
}
