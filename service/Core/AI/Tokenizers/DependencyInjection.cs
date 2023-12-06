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
        builder.Services.AddTextTokenizersCollection(collection);
        return builder;
    }

    public static IKernelMemoryBuilder WithTextTokenizerForText(
        this IKernelMemoryBuilder builder, ITextTokenizer tokenizer)
    {
        var collection = TextTokenizerCollection.Singleton();
        collection.Set(Constants.TokenizerForTextGenerator, tokenizer);
        builder.Services.AddTextTokenizersCollection(collection);
        return builder;
    }

    public static IKernelMemoryBuilder WithTextTokenizerForEmbeddings(
        this IKernelMemoryBuilder builder, ITextTokenizer tokenizer)
    {
        var collection = TextTokenizerCollection.Singleton();
        collection.Set(Constants.TokenizerForEmbeddingGenerator, tokenizer);
        builder.Services.AddTextTokenizersCollection(collection);
        return builder;
    }

    public static IKernelMemoryBuilder WithTextTokenizersCollection(
        this IKernelMemoryBuilder builder, TextTokenizerCollection instance)
    {
        builder.Services.AddTextTokenizersCollection(instance);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddTextTokenizersCollection(
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
