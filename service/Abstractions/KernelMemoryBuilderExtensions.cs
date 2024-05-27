// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.DataFormats;
using Microsoft.KernelMemory.DocumentStorage;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.KernelMemory.Pipeline.Queue;
using Microsoft.KernelMemory.Prompts;

namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions.
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    /// <summary>
    /// Configure the builder
    /// </summary>
    /// <param name="builder">KM builder instance</param>
    /// <param name="action">Action to use to configure the builder</param>
    /// <returns>Builder instance</returns>
    public static IKernelMemoryBuilder Configure(
        this IKernelMemoryBuilder builder,
        Action<IKernelMemoryBuilder> action)
    {
        action.Invoke(builder);
        return builder;
    }

    /// <summary>
    /// Configure the builder in one of two ways, depending on a condition
    /// </summary>
    /// <param name="builder">KM builder instance</param>
    /// <param name="condition">Condition to check</param>
    /// <param name="actionIfTrue">How to configure the builder when the condition is true</param>
    /// <param name="actionIfFalse">Optional, how to configure the builder when the condition is false</param>
    /// <returns>Builder instance</returns>
    public static IKernelMemoryBuilder Configure(
        this IKernelMemoryBuilder builder,
        bool condition,
        Action<IKernelMemoryBuilder> actionIfTrue,
        Action<IKernelMemoryBuilder>? actionIfFalse = null)
    {
        if (condition)
        {
            actionIfTrue.Invoke(builder);
        }
        else
        {
            actionIfFalse?.Invoke(builder);
        }

        return builder;
    }

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
        service = service ?? throw new ConfigurationException("Memory Builder: the ingestion queue client factory instance is NULL");
        builder.AddSingleton<QueueClientFactory>(service);
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomIngestionQueueClientFactory<T>(
        this IKernelMemoryBuilder builder) where T : QueueClientFactory
    {
        builder.AddSingleton<QueueClientFactory, T>();
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomDocumentStorage(
        this IKernelMemoryBuilder builder, IDocumentStorage service)
    {
        service = service ?? throw new ConfigurationException("Memory Builder: the document storage instance is NULL");
        builder.AddSingleton<IDocumentStorage>(service);
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomDocumentStorage<T>(
        this IKernelMemoryBuilder builder) where T : class, IDocumentStorage
    {
        builder.AddSingleton<IDocumentStorage, T>();
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomMimeTypeDetection(
        this IKernelMemoryBuilder builder, IMimeTypeDetection service)
    {
        service = service ?? throw new ConfigurationException("Memory Builder: the MIME type detection instance is NULL");
        builder.AddSingleton<IMimeTypeDetection>(service);
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomMimeTypeDetection<T>(
        this IKernelMemoryBuilder builder) where T : class, IMimeTypeDetection
    {
        builder.AddSingleton<IMimeTypeDetection, T>();
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomEmbeddingGenerator(
        this IKernelMemoryBuilder builder,
        ITextEmbeddingGenerator service,
        bool useForIngestion = true,
        bool useForRetrieval = true)
    {
        service = service ?? throw new ConfigurationException("Memory Builder: the embedding generator instance is NULL");

        if (useForRetrieval)
        {
            builder.AddSingleton<ITextEmbeddingGenerator>(service);
        }

        if (useForIngestion)
        {
            builder.AddIngestionEmbeddingGenerator(service);
        }

        return builder;
    }

    public static IKernelMemoryBuilder WithCustomEmbeddingGenerator<T>(
        this IKernelMemoryBuilder builder) where T : class, ITextEmbeddingGenerator
    {
        builder.AddSingleton<ITextEmbeddingGenerator, T>();
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomMemoryDb(
        this IKernelMemoryBuilder builder,
        IMemoryDb service,
        bool useForIngestion = true,
        bool useForRetrieval = true)
    {
        service = service ?? throw new ConfigurationException("Memory Builder: the memory DB instance is NULL");

        if (useForRetrieval)
        {
            builder.AddSingleton<IMemoryDb>(service);
        }

        if (useForIngestion)
        {
            builder.AddIngestionMemoryDb(service);
        }

        return builder;
    }

    public static IKernelMemoryBuilder WithCustomMemoryDb<T>(
        this IKernelMemoryBuilder builder) where T : class, IMemoryDb
    {
        builder.AddSingleton<IMemoryDb, T>();
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomTextGenerator(
        this IKernelMemoryBuilder builder,
        ITextGenerator service)
    {
        service = service ?? throw new ConfigurationException("Memory Builder: the text generator instance is NULL");
        builder.AddSingleton<ITextGenerator>(service);
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomTextGenerator<T>(
        this IKernelMemoryBuilder builder) where T : class, ITextGenerator
    {
        builder.AddSingleton<ITextGenerator, T>();
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomImageOcr(
        this IKernelMemoryBuilder builder,
        IOcrEngine service)
    {
        service = service ?? throw new ConfigurationException("Memory Builder: the OCR engine instance is NULL");
        builder.AddSingleton<IOcrEngine>(service);
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomImageOcr<T>(
        this IKernelMemoryBuilder builder) where T : class, IOcrEngine
    {
        builder.AddSingleton<IOcrEngine, T>();
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomPromptProvider(
        this IKernelMemoryBuilder builder, IPromptProvider service)
    {
        service = service ?? throw new ConfigurationException("Memory Builder: the prompt provider instance is NULL");
        builder.AddSingleton<IPromptProvider>(service);
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomPromptProvider<T>(
        this IKernelMemoryBuilder builder) where T : class, IPromptProvider
    {
        builder.AddSingleton<IPromptProvider, T>();
        return builder;
    }

    /// <summary>
    /// Customize how text extracted from documents is partitioned in smaller chunks.
    /// </summary>
    /// <param name="builder">KM builder instance</param>
    /// <param name="options">Partitioning options</param>
    public static IKernelMemoryBuilder WithCustomTextPartitioningOptions(
        this IKernelMemoryBuilder builder, TextPartitioningOptions options)
    {
        options = options ?? throw new ConfigurationException("Memory Builder: the given text partitioning options are NULL");
        builder.With<TextPartitioningOptions>(options);
        return builder;
    }
}
