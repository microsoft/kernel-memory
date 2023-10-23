// Copyright (c) Microsoft. All rights reserved.

using System.Runtime.CompilerServices;
using Microsoft.SemanticMemory;
using Microsoft.SemanticMemory.AI;

public static class Program
{
    public static void Main()
    {
        var llamaConfig = new LlamaConfig
        {
            // ...
        };

        var memory = new MemoryClientBuilder()
            .WithCustomTextGeneration(new LlamaTextGeneration(llamaConfig))
            .FromAppSettings() // read "KernelMemory" settings from appsettings.json
            .Build();

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
