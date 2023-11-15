// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.SemanticKernel.AI.Embeddings;

public static class Program
{
    public static void Main()
    {
        var memory = new KernelMemoryBuilder()
            .WithCustomEmbeddingGeneration(new MyCustomEmbeddingGenerator())
            .FromAppSettings() // read "KernelMemory" settings from appsettings.json
            .Build();

        // ...
    }
}

public class MyCustomEmbeddingGenerator : ITextEmbeddingGeneration
{
    /// <summary>
    /// Generates embeddings for the given data.
    /// </summary>
    /// <param name="data">List of strings to generate embeddings for</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>List of embeddings</returns>
    public Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IList<string> data,
        CancellationToken cancellationToken = new())
    {
        // Your code here: loop through the list of strings in `data`,
        // generate embedding vectors, collect and return the list of embeddings.

        throw new NotImplementedException();
    }
}
