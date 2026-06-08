// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.LlamaSharp;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithLlamaTextGeneration(
        this IKernelMemoryBuilder builder,
        string modelPath,
        uint maxTokenTotal,
        ITextTokenizer? textTokenizer = null)
    {
        var config = new LlamaSharpModelConfig
        {
            ModelPath = modelPath,
            MaxTokenTotal = maxTokenTotal
        };

        builder.Services.AddLlamaSharpTextGeneration(config, textTokenizer);

        return builder;
    }

    public static IKernelMemoryBuilder WithLlamaTextEmbeddingGeneration(
        this IKernelMemoryBuilder builder,
        string modelPath,
        uint maxTokenTotal,
        ITextTokenizer? textTokenizer = null)
    {
        var config = new LlamaSharpModelConfig
        {
            ModelPath = modelPath,
            MaxTokenTotal = maxTokenTotal
        };

        builder.Services.AddLlamaSharpTextEmbeddingGeneration(config, textTokenizer);

        return builder;
    }

    public static IKernelMemoryBuilder WithLlamaTextGeneration(
        this IKernelMemoryBuilder builder,
        LlamaSharpModelConfig config,
        ITextTokenizer? textTokenizer = null)
    {
        builder.Services.AddLlamaSharpTextGeneration(config, textTokenizer);
        return builder;
    }

    public static IKernelMemoryBuilder WithLlamaTextEmbeddingGeneration(
        this IKernelMemoryBuilder builder,
        LlamaSharpModelConfig config,
        ITextTokenizer? textTokenizer = null)
    {
        builder.Services.AddLlamaSharpTextEmbeddingGeneration(config, textTokenizer);
        return builder;
    }

    public static IKernelMemoryBuilder WithLlamaTextGeneration(
        this IKernelMemoryBuilder builder,
        LlamaSharpConfig config,
        ITextTokenizer? textTokenizer = null)
    {
        builder.Services.AddLlamaSharpTextGeneration(config.TextModel, textTokenizer);
        return builder;
    }

    public static IKernelMemoryBuilder WithLlamaTextEmbeddingGeneration(
        this IKernelMemoryBuilder builder,
        LlamaSharpConfig config,
        ITextTokenizer? textTokenizer = null)
    {
        builder.Services.AddLlamaSharpTextEmbeddingGeneration(config.EmbeddingModel, textTokenizer);
        return builder;
    }
}

/// <summary>
/// .NET IServiceCollection dependency injection extensions.
/// </summary>
public static partial class DependencyInjection
{
    public static IServiceCollection AddLlamaSharpTextGeneration(
        this IServiceCollection services,
        LlamaSharpModelConfig config,
        ITextTokenizer? textTokenizer = null)
    {
        config.Validate();
        return services
            .AddSingleton<ITextGenerator, LlamaSharpTextGenerator>(serviceProvider => new LlamaSharpTextGenerator(
                config: config,
                textTokenizer: textTokenizer,
                loggerFactory: serviceProvider.GetService<ILoggerFactory>()));
    }

    public static IServiceCollection AddLlamaSharpTextEmbeddingGeneration(
        this IServiceCollection services,
        LlamaSharpModelConfig config,
        ITextTokenizer? textTokenizer = null)
    {
        config.Validate();
        return services
            .AddSingleton<ITextEmbeddingGenerator, LlamaSharpTextEmbeddingGenerator>(serviceProvider => new LlamaSharpTextEmbeddingGenerator(
                config: config,
                textTokenizer: textTokenizer,
                loggerFactory: serviceProvider.GetService<ILoggerFactory>()));
    }
}
