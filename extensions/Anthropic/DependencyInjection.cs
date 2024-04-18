// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.KernelMemory.AI.Anthropic;

/// <summary>
/// Allows configuration for Anthropic text generation
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    /// <summary>
    /// Configure Kernel Memory to use Anthropic text generation to answer
    /// RAG questions.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="config"></param>
    /// <param name="textTokenizer">Optional tokenizer, default one will be used if passed null.</param>
    /// <returns></returns>
    public static IKernelMemoryBuilder WithAnthropicTextGeneration(
        this IKernelMemoryBuilder builder,
        AnthropicConfiguration config,
        ITextTokenizer? textTokenizer = null)
    {
        builder.Services.AddAnthropicTextGeneration(config, textTokenizer);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddAnthropicTextGeneration(
        this IServiceCollection services,
        AnthropicConfiguration config,
        ITextTokenizer? textTokenizer)
    {
        services.AddSingleton(config);
        return services
           .AddSingleton<ITextGenerator>(serviceProvider => new AnthropicTextGeneration(
               httpClientFactory: serviceProvider.GetService<IHttpClientFactory>()!,
               config: config,
               textTokenizer: textTokenizer,
               log: serviceProvider.GetService<ILogger<AnthropicTextGeneration>>()));
    }
}
