// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.Onnx;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithOnnxTextGeneration(
        this IKernelMemoryBuilder builder,
        string modelPath,
        uint maxTokenTotal,
        ITextTokenizer? textTokenizer = null)
    {
        var config = new OnnxConfig
        {
            TextModelDir = modelPath
        };

        builder.Services.AddOnnxTextGeneration(config, textTokenizer);

        return builder;
    }

    public static IKernelMemoryBuilder WithOnnxTextGeneration(
        this IKernelMemoryBuilder builder,
        OnnxConfig config,
        ITextTokenizer? textTokenizer = null)
    {
        builder.Services.AddOnnxTextGeneration(config, textTokenizer);
        return builder;
    }
}

/// <summary>
/// .NET IServiceCollection dependency injection extensions.
/// </summary>
public static partial class DependencyInjection
{
    public static IServiceCollection AddOnnxTextGeneration(
        this IServiceCollection services,
        OnnxConfig config,
        ITextTokenizer? textTokenizer = null)
    {
        config.Validate();
        return services
            .AddSingleton<ITextGenerator, OnnxTextGenerator>(serviceProvider => new OnnxTextGenerator(
                config: config,
                textTokenizer: textTokenizer,
                loggerFactory: serviceProvider.GetService<ILoggerFactory>()));
    }
}
