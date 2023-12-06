// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.KernelMemory.AI.Tokenizers;

public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithTextTokenizer(
        this IKernelMemoryBuilder builder, ITextTokenizer tokenizer)
    {
        var collection = TextTokenizerCollection.Singleton();
        collection.Set(Constants.TokenizerForTextGenerator, tokenizer);
        collection.Set(Constants.TokenizerForEmbeddingGenerator, tokenizer);
        builder.Services.AddTextTokenizerCollection(collection);
        return builder;
    }

    public static IKernelMemoryBuilder WithTextTokenizers(
        this IKernelMemoryBuilder builder, TextTokenizerCollection instance)
    {
        builder.Services.AddTextTokenizerCollection(instance);
        return builder;
    }

    public static IKernelMemoryBuilder WithTextTokenizerForTextGeneration(
        this IKernelMemoryBuilder builder, ITextTokenizer tokenizer)
    {
        var collection = TextTokenizerCollection.Singleton();
        collection.Set(Constants.TokenizerForTextGenerator, tokenizer);
        builder.Services.AddTextTokenizerCollection(collection);
        return builder;
    }

    public static IKernelMemoryBuilder WithTextTokenizerForEmbeddingGeneration(
        this IKernelMemoryBuilder builder, ITextTokenizer tokenizer)
    {
        var collection = TextTokenizerCollection.Singleton();
        collection.Set(Constants.TokenizerForEmbeddingGenerator, tokenizer);
        builder.Services.AddTextTokenizerCollection(collection);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddTextTokenizerCollection(
        this IServiceCollection services, TextTokenizerCollection? instance = null)
    {
        if (instance == null)
        {
            instance = TextTokenizerCollection.Singleton();
            instance.Set(Constants.TokenizerForTextGenerator, new DefaultGPTTokenizer());
            instance.Set(Constants.TokenizerForEmbeddingGenerator, new DefaultGPTTokenizer());
        }

        services.AddSingleton<TextTokenizerCollection>(instance);
        return services;
    }
}
