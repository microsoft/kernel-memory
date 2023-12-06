// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Configuration;
using Microsoft.SemanticKernel.AI.TextGeneration;

namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions.
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    /// <summary>
    /// Inject an implementation of <see cref="ITextGenerationService">SK text generation service</see>
    /// for local dependencies on <see cref="ITextGeneration"/>
    /// </summary>
    /// <param name="builder">KM builder</param>
    /// <param name="service">SK text generation service instance</param>
    /// <returns>KM builder</returns>
    public static IKernelMemoryBuilder WithSemanticKernelTextGenerationService(
        this IKernelMemoryBuilder builder,
        ITextGenerationService service)
    {
        service = service ?? throw new ConfigurationException("The semantic kernel text generation service instance is NULL");
        return builder.AddSingleton<ITextGeneration>(new SemanticKernelTextGeneration(service));
    }
}
