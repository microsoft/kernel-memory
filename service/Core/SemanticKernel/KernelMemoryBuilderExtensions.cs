// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.TextGeneration;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions.
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    /// <summary>
    /// Inject an implementation of <see cref="ITextGenerationService">SK text generation service</see>
    /// for local dependencies on <see cref="ITextGenerator"/>
    /// </summary>
    /// <param name="builder">KM builder</param>
    /// <param name="service">SK text generation service instance</param>
    /// <param name="config">SK text generator settings</param>
    /// <param name="textTokenizer">Tokenizer used to count tokens used by prompts</param>
    ///  <param name="loggerFactory">.NET logger factory</param>
    /// <returns>KM builder</returns>
    public static IKernelMemoryBuilder WithSemanticKernelTextGenerationService(
        this IKernelMemoryBuilder builder,
        ITextGenerationService service,
        SemanticKernelConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
    {
        if (service == null) { throw new ConfigurationException("Memory Builder: the semantic kernel text generation service instance is NULL"); }

        return builder.AddSingleton<ITextGenerator>(new SemanticKernelTextGenerator(service, config, textTokenizer, loggerFactory));
    }

    ///  <summary>
    /// Inject an implementation of<see cref="ITextEmbeddingGenerationService">SK text embedding generation service</see>
    ///  for local dependencies on <see cref="ITextEmbeddingGenerator"/>
    ///  </summary>
    ///  <param name="builder">KM builder</param>
    ///  <param name="service">SK text embedding generation instance</param>
    ///  <param name="config">SK text embedding generator settings</param>
    ///  <param name="textTokenizer">Tokenizer used to count tokens sent to the embedding generator</param>
    ///  <param name="loggerFactory">.NET logger factory</param>
    ///  <param name="onlyForRetrieval">Whether to use this embedding generator only during data ingestion, and not for retrieval (search and ask API)</param>
    ///  <returns>KM builder</returns>
    public static IKernelMemoryBuilder WithSemanticKernelTextEmbeddingGenerationService(
        this IKernelMemoryBuilder builder,
        ITextEmbeddingGenerationService service,
        SemanticKernelConfig config,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null,
        bool onlyForRetrieval = false)
    {
        if (service == null) { throw new ConfigurationException("Memory Builder: the semantic kernel text embedding generation service instance is NULL"); }

        var generator = new SemanticKernelTextEmbeddingGenerator(service, config, textTokenizer, loggerFactory);
        builder.AddSingleton<ITextEmbeddingGenerator>(generator);

        if (!onlyForRetrieval)
        {
            builder.AddIngestionEmbeddingGenerator(generator);
        }

        return builder;
    }
}
