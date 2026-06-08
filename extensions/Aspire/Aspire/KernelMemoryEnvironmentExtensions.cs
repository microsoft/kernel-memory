// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.KernelMemory.Configuration;

namespace Microsoft.KernelMemory.Aspire;

public static class KernelMemoryEnvironmentExtensions
{
    public static IResourceBuilder<T> WithKmServiceConfig<T>(
        this IResourceBuilder<T> builder, string serviceName, object config)
        where T : IResourceWithEnvironment
    {
        return builder.WithKmConfig(config, "KernelMemory", "Services", serviceName);
    }

    public static IResourceBuilder<T> WithKmConfig<T>(
        this IResourceBuilder<T> builder, object config, params string[] parents)
        where T : IResourceWithEnvironment
    {
        Dictionary<string, string> result = ConfigEnvVars.GenerateEnvVarsFromObject(config, parents);
        foreach (var v in result)
        {
            builder.WithEnvironment(v.Key, v.Value);
        }

        return builder;
    }

    public static IResourceBuilder<T> WithKmTextEmbeddingGenerationEnvironment<T>(
        this IResourceBuilder<T> builder, string? serviceName, object? config = null)
        where T : IResourceWithEnvironment
    {
        builder
            .WithEnvironment("KernelMemory__DataIngestion__EmbeddingGeneratorTypes__0", serviceName ?? "")
            .WithEnvironment("KernelMemory__Retrieval__EmbeddingGeneratorType", serviceName ?? "");
        if (serviceName != null && config != null)
        {
            builder.WithKmServiceConfig(serviceName, config);
        }

        return builder;
    }

    public static IResourceBuilder<T> WithKmTextGenerationEnvironment<T>(
        this IResourceBuilder<T> builder, string? serviceName, object? config = null)
        where T : IResourceWithEnvironment
    {
        builder.WithEnvironment("KernelMemory__TextGeneratorType", serviceName ?? "");
        if (serviceName != null && config != null)
        {
            builder.WithKmServiceConfig(serviceName, config);
        }

        return builder;
    }

    public static IResourceBuilder<T> WithKmDocumentStorageEnvironment<T>(
        this IResourceBuilder<T> builder, string? serviceName, object? config = null)
        where T : IResourceWithEnvironment
    {
        builder.WithEnvironment("KernelMemory__DocumentStorageType", serviceName ?? "");
        if (serviceName != null && config != null)
        {
            builder.WithKmServiceConfig(serviceName, config);
        }

        return builder;
    }

    public static IResourceBuilder<T> WithKmMemoryDbEnvironment<T>(
        this IResourceBuilder<T> builder, string? serviceName, object? config = null)
        where T : IResourceWithEnvironment
    {
        builder
            .WithEnvironment("KernelMemory__DataIngestion__MemoryDbTypes__0", serviceName ?? "")
            .WithEnvironment("KernelMemory__Retrieval__MemoryDbType", serviceName ?? "");
        if (serviceName != null && config != null)
        {
            builder.WithKmServiceConfig(serviceName, config);
        }

        return builder;
    }

    public static IResourceBuilder<T> WithKmOrchestrationEnvironment<T>(
        this IResourceBuilder<T> builder, string? serviceName, object? config = null)
        where T : IResourceWithEnvironment
    {
        builder.WithEnvironment("KernelMemory__DataIngestion__DistributedOrchestration__QueueType", serviceName ?? "");
        if (serviceName != null && config != null)
        {
            builder.WithKmServiceConfig(serviceName, config);
        }

        return builder;
    }

    public static IResourceBuilder<T> WithKmContentSafetyModerationEnvironment<T>(
        this IResourceBuilder<T> builder, string? serviceName, object? config = null)
        where T : IResourceWithEnvironment
    {
        builder.WithEnvironment("KernelMemory__ContentModerationType", serviceName ?? "");
        if (serviceName != null && config != null)
        {
            builder.WithKmServiceConfig(serviceName, config);
        }

        return builder;
    }

    public static IResourceBuilder<T> WithKmOcrEnvironment<T>(
        this IResourceBuilder<T> builder, string? serviceName, object? config = null)
        where T : IResourceWithEnvironment
    {
        builder.WithEnvironment("KernelMemory__DataIngestion__ImageOcrType", serviceName ?? "");
        if (serviceName != null && config != null)
        {
            builder.WithKmServiceConfig(serviceName, config);
        }

        return builder;
    }
}
