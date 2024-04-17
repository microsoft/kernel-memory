// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;

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
    public static IKernelMemoryBuilder WithAnthropicTextGeneration(
        this IKernelMemoryBuilder builder,
        AnthropicConfiguration config)
    {
        builder.Services.AddAnthropicTextGeneration(config);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddAnthropicTextGeneration(
        this IServiceCollection services,
        AnthropicConfiguration config)
    {
        services.AddSingleton(config);
        return services.AddSingleton<ITextGenerator, AnthropicTextGeneration>();
    }
}
