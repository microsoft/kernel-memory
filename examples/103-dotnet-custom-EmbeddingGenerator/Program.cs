// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;

public static class Program
{
    public static void Main()
    {
        var myConfig = new MyEmbeddingGeneratorConfig
        {
            MaxToken = 4096
        };

        var azureOpenAITextConfig = new AzureOpenAIConfig();

        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.development.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build()
            .BindSection("KernelMemory:Services:AzureOpenAIText", azureOpenAITextConfig);

        var memory = new KernelMemoryBuilder()
            .WithAzureOpenAITextGeneration(azureOpenAITextConfig)
            .WithCustomEmbeddingGenerator(new MyEmbeddingGenerator(myConfig))
            .Build();

        // ...
    }
}

public class MyEmbeddingGeneratorConfig
{
    public int MaxToken { get; set; } = 4096;
}

public class MyEmbeddingGenerator : ITextEmbeddingGenerator
{
    public MyEmbeddingGenerator(MyEmbeddingGeneratorConfig embeddingGeneratorConfig)
    {
        this.MaxTokens = embeddingGeneratorConfig.MaxToken;
    }

    /// <inheritdoc />
    public int MaxTokens { get; }

    /// <inheritdoc />
    public int CountTokens(string text)
    {
        // ... calculate and return the number of tokens ...

        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetTokens(string text)
    {
        // ... calculate and return the list of tokens ...

        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<Embedding> GenerateEmbeddingAsync(
        string text, CancellationToken cancellationToken = default)
    {
        // ... generate and return the embedding for the given text ...

        throw new NotImplementedException();
    }
}
