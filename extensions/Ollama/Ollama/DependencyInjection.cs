// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.Ollama;
using OllamaSharp;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithOllamaTextGeneration(
        this IKernelMemoryBuilder builder,
        OllamaConfig config,
        ITextTokenizer? textTokenizer = null)
    {
        builder.Services.AddOllamaTextGeneration(config, textTokenizer);
        return builder;
    }

    public static IKernelMemoryBuilder WithOllamaTextGeneration(
        this IKernelMemoryBuilder builder,
        string modelName,
        string endpoint = "http://localhost:11434",
        ITextTokenizer? textTokenizer = null)
    {
        builder.Services.AddOllamaTextGeneration(modelName, endpoint, textTokenizer);
        return builder;
    }

    public static IKernelMemoryBuilder WithOllamaTextEmbeddingGeneration(
        this IKernelMemoryBuilder builder,
        OllamaConfig config,
        ITextTokenizer? textTokenizer = null)
    {
        builder.Services.AddOllamaTextEmbeddingGeneration(config, textTokenizer);
        return builder;
    }

    public static IKernelMemoryBuilder WithOllamaTextEmbeddingGeneration(
        this IKernelMemoryBuilder builder,
        string modelName,
        string endpoint = "http://localhost:11434",
        ITextTokenizer? textTokenizer = null)
    {
        builder.Services.AddOllamaTextEmbeddingGeneration(modelName, endpoint, textTokenizer);
        return builder;
    }
}

/// <summary>
/// .NET IServiceCollection dependency injection extensions.
/// </summary>
public static partial class DependencyInjection
{
    public static IServiceCollection AddOllamaTextGeneration(
        this IServiceCollection services,
        string modelName,
        string endpoint = "http://localhost:11434",
        ITextTokenizer? textTokenizer = null)
    {
        return services
            .AddSingleton<ITextGenerator>(
                serviceProvider => new OllamaTextGenerator(
                    new OllamaApiClient(new Uri(endpoint), modelName),
                    new OllamaModelConfig { ModelName = modelName },
                    textTokenizer,
                    serviceProvider.GetService<ILoggerFactory>()));
    }

    public static IServiceCollection AddOllamaTextGeneration(
        this IServiceCollection services,
        OllamaConfig config,
        ITextTokenizer? textTokenizer = null)
    {
        return services
            .AddSingleton<ITextGenerator>(
                serviceProvider => new OllamaTextGenerator(
                    new OllamaApiClient(new Uri(config.Endpoint), config.TextModel.ModelName),
                    config.TextModel,
                    textTokenizer,
                    serviceProvider.GetService<ILoggerFactory>()));
    }

    public static IServiceCollection AddOllamaTextEmbeddingGeneration(
        this IServiceCollection services,
        string modelName,
        string endpoint = "http://localhost:11434",
        ITextTokenizer? textTokenizer = null)
    {
        return services
            .AddSingleton<ITextEmbeddingGenerator>(
                serviceProvider => new OllamaTextEmbeddingGenerator(
                    new OllamaApiClient(new Uri(endpoint), modelName),
                    new OllamaModelConfig { ModelName = modelName },
                    textTokenizer,
                    serviceProvider.GetService<ILoggerFactory>()));
    }

    public static IServiceCollection AddOllamaTextEmbeddingGeneration(
        this IServiceCollection services,
        OllamaConfig config,
        ITextTokenizer? textTokenizer = null)
    {
        return services
            .AddSingleton<ITextEmbeddingGenerator>(
                serviceProvider => new OllamaTextEmbeddingGenerator(
                    new OllamaApiClient(new Uri(config.Endpoint), config.EmbeddingModel.ModelName),
                    config.EmbeddingModel,
                    textTokenizer,
                    serviceProvider.GetService<ILoggerFactory>()));
    }
}
