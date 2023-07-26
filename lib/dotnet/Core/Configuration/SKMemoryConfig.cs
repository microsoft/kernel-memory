// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Microsoft.SemanticKernel.SemanticMemory.Core.Configuration;

public class SKMemoryConfig
{
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

        // return section.GetSection(sectionName).Get<T>() ?? new T();
        return section.Get<T>() ?? new T();
    }
}
