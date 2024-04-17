// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using SemanticMemory.Extensions.Anthropic;

namespace Microsoft.KernelMemory.AI.Anthropic;

/// <summary>
/// Allows configuration for Anthropic text generation
/// </summary>
public static class AnthropicDependencyInjection
{
    /// <summary>
    /// Configure Kernel Memory to use Anthropic text generation to answer
    /// RAG questions.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IKernelMemoryBuilder WithAnthropicTextGeneration(
        this IKernelMemoryBuilder builder,
        AnthropicTextGenerationConfiguration config)
    {
        builder.Services.AddAnthropicTextGeneration(config);
        return builder;
    }

    private static IServiceCollection AddAnthropicTextGeneration(
        this IServiceCollection services,
        AnthropicTextGenerationConfiguration config)
    {
        services.AddSingleton(config);
        return services.AddSingleton<ITextGenerator, AnthropicTextGeneration>();
    }
}
