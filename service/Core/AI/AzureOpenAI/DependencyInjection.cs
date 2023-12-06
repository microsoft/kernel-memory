// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.AzureOpenAI;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithAzureOpenAITextEmbeddingGeneration(
        this IKernelMemoryBuilder builder,
        AzureOpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null,
        bool onlyForRetrieval = false)
    {
        builder.Services.AddAzureOpenAIEmbeddingGeneration(config, textTokenizer);

        if (!onlyForRetrieval)
        {
            builder.AddIngestionEmbeddingGenerator(
                new AzureOpenAITextEmbeddingGenerator(
                    config: config,
                    textTokenizer: textTokenizer,
                    loggerFactory: loggerFactory));
        }

        return builder;
    }

    public static IKernelMemoryBuilder WithAzureOpenAITextGeneration(
        this IKernelMemoryBuilder builder,
        AzureOpenAIConfig config,
        ITextTokenizer? textTokenizer = null)
    {
        builder.Services.AddAzureOpenAITextGeneration(config, textTokenizer);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureOpenAIEmbeddingGeneration(
        this IServiceCollection services,
        AzureOpenAIConfig config,
        ITextTokenizer? textTokenizer = null)
    {
        return services
            .AddSingleton<ITextEmbeddingGenerator>(serviceProvider => new AzureOpenAITextEmbeddingGenerator(
                config,
                textTokenizer: textTokenizer,
                loggerFactory: serviceProvider.GetService<ILoggerFactory>()));
    }

    public static IServiceCollection AddAzureOpenAITextGeneration(
        this IServiceCollection services,
        AzureOpenAIConfig config,
        ITextTokenizer? textTokenizer = null)
    {
        return services
            .AddSingleton<ITextGenerator>(serviceProvider => new AzureOpenAITextGenerator(
                config: config,
                textTokenizer: textTokenizer,
                log: serviceProvider.GetService<ILogger<AzureOpenAITextGenerator>>()));
    }
}
