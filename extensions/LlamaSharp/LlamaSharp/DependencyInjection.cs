﻿// Copyright (c) Microsoft. All rights reserved.

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
        var config = new LlamaSharpConfig
        {
            ModelPath = modelPath,
            MaxTokenTotal = maxTokenTotal
        };

        builder.Services.AddLlamaTextGeneration(config, textTokenizer);

        return builder;
    }

    public static IKernelMemoryBuilder WithLlamaTextGeneration(
        this IKernelMemoryBuilder builder,
        LlamaSharpConfig config,
        ITextTokenizer? textTokenizer = null)
    {
        builder.Services.AddLlamaTextGeneration(config, textTokenizer);
        return builder;
    }
}

/// <summary>
/// .NET IServiceCollection dependency injection extensions.
/// </summary>
public static partial class DependencyInjection
{
    public static IServiceCollection AddLlamaTextGeneration(
        this IServiceCollection services,
        LlamaSharpConfig config,
        ITextTokenizer? textTokenizer = null)
    {
        config.Validate();
        return services
            .AddSingleton<ITextGenerator, LlamaSharpTextGenerator>(serviceProvider => new LlamaSharpTextGenerator(
                config: config,
                textTokenizer: textTokenizer,
                loggerFactory: serviceProvider.GetService<ILoggerFactory>()));
    }
}
