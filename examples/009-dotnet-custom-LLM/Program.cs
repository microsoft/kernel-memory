// Copyright (c) Microsoft. All rights reserved.

using System.Runtime.CompilerServices;
using Microsoft.SemanticMemory;
using Microsoft.SemanticMemory.AI;
using Microsoft.SemanticMemory.MemoryStorage.Qdrant;

public static class Program
{
    public static void Main()
    {
        var llamaConfig = new LlamaConfig
        {
            // ...
        };

        var openAIConfig = new OpenAIConfig
        {
            EmbeddingModel = "text-embedding-ada-002",
            APIKey = Env.Var("OPENAI_API_KEY")
        };

        var memory = new MemoryClientBuilder()
            .WithCustomTextGeneration(new LlamaTextGeneration(llamaConfig))
            .WithOpenAITextEmbedding(openAIConfig)
            .WithQdrant(new QdrantConfig
            {
                /* ... */
            });

        // ...
    }
}

public class LlamaConfig
{
    // ...
}

public class LlamaTextGeneration : ITextGeneration
{
    private readonly LlamaConfig _config;

    public LlamaTextGeneration(LlamaConfig config)
    {
        this._config = config;
    }

    public async IAsyncEnumerable<string> GenerateTextAsync(
        string prompt,
        TextGenerationOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = new())
    {
        // ...

        await Task.Delay(0, cancellationToken).ConfigureAwait(false);

        yield return "some text";
    }
}
