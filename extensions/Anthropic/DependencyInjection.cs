// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.Anthropic;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    /// <summary>
    /// Configure Kernel Memory to use Anthropic text generation.
    /// </summary>
    /// <param name="builder">KernelMemory builder</param>
    /// <param name="config">Anthropic configuration</param>
    /// <param name="textTokenizer">Optional tokenizer, default one will be used if passed null.</param>
    public static IKernelMemoryBuilder WithAnthropicTextGeneration(
        this IKernelMemoryBuilder builder,
        AnthropicConfig config,
        ITextTokenizer? textTokenizer = null)
    {
        builder.Services.AddAnthropicTextGeneration(config, textTokenizer);
        return builder;
    }
}

/// <summary>
/// .NET IServiceCollection dependency injection extensions.
/// </summary>
public static partial class DependencyInjection
{
    /// <summary>
    /// Configure Kernel Memory to use Anthropic text generation.
    /// </summary>
    /// <param name="services">Application services collection</param>
    /// <param name="config">Anthropic settings</param>
    /// <param name="textTokenizer">Tokenizer to measure content size</param>
    public static IServiceCollection AddAnthropicTextGeneration(
        this IServiceCollection services,
        AnthropicConfig config,
        ITextTokenizer? textTokenizer = null)
    {
        services.AddSingleton(config);

        if (textTokenizer != null)
        {
            return services
                .AddSingleton<ITextGenerator>(serviceProvider => new AnthropicTextGeneration(
                    config: config,
                    textTokenizer: textTokenizer,
                    httpClientFactory: serviceProvider.GetService<IHttpClientFactory>(),
                    loggerFactory: serviceProvider.GetService<ILoggerFactory>()));
        }

        return services.AddSingleton<ITextGenerator, AnthropicTextGeneration>();
    }
}
