// Copyright (c) Microsoft. All rights reserved.

using System.Runtime.CompilerServices;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;

public static class Program
{
    public static void Main()
    {
        var customModelConfig = new CustomModelConfig
        {
            MaxToken = 4096,
            ModelPath = "...",
        };

        var azureOpenAIEmbeddingConfig = new AzureOpenAIConfig();

        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build()
            .BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAIEmbeddingConfig);

        var memory = new KernelMemoryBuilder()
            .WithCustomTextGenerator(new CustomModelTextGeneration(customModelConfig))
            .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig)
            .Build();

        // ...
    }
}

public class CustomModelConfig
{
    public string ModelPath { get; set; } = "";
    public int MaxToken { get; set; } = 4096;
}

public class CustomModelTextGeneration : ITextGenerator
{
    private readonly CustomModelConfig _config;

    public CustomModelTextGeneration(CustomModelConfig config)
    {
        this._config = config;
        this.MaxTokenTotal = config.MaxToken;
    }

    /// <inheritdoc />
    public int MaxTokenTotal { get; }

    /// <inheritdoc />
    public int CountTokens(string text)
    {
        // ... calculate and return the number of tokens ...

        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetTokens(string text)
    {
        // ... calculate and return list of tokens ...

        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> GenerateTextAsync(
        string prompt,
        TextGenerationOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // ... generate and return the text from the given prompt ...

        // Remove this
        await Task.Delay(0, cancellationToken).ConfigureAwait(false);

        yield return "some text";
    }
}
