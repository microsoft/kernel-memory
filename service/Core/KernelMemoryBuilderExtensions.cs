// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.ContentStorage;
using Microsoft.KernelMemory.DataFormats.Image;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.KernelMemory.Pipeline.Queue;
using Microsoft.KernelMemory.Prompts;
using Microsoft.SemanticKernel.AI.Embeddings;

namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions.
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    /// <summary>
    /// Allows to inject any dependency into the builder, e.g. options for handlers
    /// and custom components used by the system
    /// </summary>
    /// <param name="builder">KM builder instance</param>
    /// <param name="dependency">Dependency. Can be NULL.</param>
    /// <typeparam name="T">Type of dependency</typeparam>
    public static IKernelMemoryBuilder With<T>(
        this IKernelMemoryBuilder builder, T dependency) where T : class, new()
    {
        builder.AddSingleton(dependency);
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomIngestionQueueClientFactory(
        this IKernelMemoryBuilder builder, QueueClientFactory service)
    {
        service = service ?? throw new ConfigurationException("The ingestion queue client factory instance is NULL");
        builder.AddSingleton<QueueClientFactory>(service);
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomStorage(
        this IKernelMemoryBuilder builder, IContentStorage service)
    {
        service = service ?? throw new ConfigurationException("The content storage instance is NULL");
        builder.AddSingleton<IContentStorage>(service);
        return builder;
    }

    public static IKernelMemoryBuilder WithDefaultMimeTypeDetection(
        this IKernelMemoryBuilder builder)
    {
        builder.AddSingleton<IMimeTypeDetection, MimeTypesDetection>();
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomMimeTypeDetection(
        this IKernelMemoryBuilder builder, IMimeTypeDetection service)
    {
        service = service ?? throw new ConfigurationException("The MIME type detection instance is NULL");
        builder.AddSingleton<IMimeTypeDetection>(service);
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomEmbeddingGeneration(
        this IKernelMemoryBuilder builder,
        ITextEmbeddingGeneration service,
        bool useForIngestion = true,
        bool useForRetrieval = true)
    {
        service = service ?? throw new ConfigurationException("The embedding generator instance is NULL");

        if (useForRetrieval)
        {
            builder.AddSingleton<ITextEmbeddingGeneration>(service);
        }

        if (useForIngestion)
        {
            builder.AddIngestionEmbeddingGenerator(service);
        }

        return builder;
    }

    public static IKernelMemoryBuilder WithCustomVectorDb(
        this IKernelMemoryBuilder builder,
        IVectorDb service,
        bool useForIngestion = true,
        bool useForRetrieval = true)
    {
        service = service ?? throw new ConfigurationException("The vector DB instance is NULL");

        if (useForRetrieval)
        {
            builder.AddSingleton<IVectorDb>(service);
        }

        if (useForIngestion)
        {
            builder.AddIngestionVectorDb(service);
        }

        return builder;
    }

    public static IKernelMemoryBuilder WithCustomTextGeneration(
        this IKernelMemoryBuilder builder,
        ITextGeneration service)
    {
        service = service ?? throw new ConfigurationException("The text generator instance is NULL");
        builder.AddSingleton<ITextGeneration>(service);
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomImageOcr(
        this IKernelMemoryBuilder builder,
        IOcrEngine service)
    {
        service = service ?? throw new ConfigurationException("The OCR engine instance is NULL");
        builder.AddSingleton<IOcrEngine>(service);
        return builder;
    }

    public static IKernelMemoryBuilder WithDefaultPromptProvider(
        this IKernelMemoryBuilder builder)
    {
        builder.AddSingleton<IPromptProvider, EmbeddedPromptProvider>();
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomPromptProvider(
        this IKernelMemoryBuilder builder, IPromptProvider service)
    {
        service = service ?? throw new ConfigurationException("The prompt provider instance is NULL");
        builder.AddSingleton<IPromptProvider>(service);
        return builder;
    }
}
