// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.Llama;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

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
