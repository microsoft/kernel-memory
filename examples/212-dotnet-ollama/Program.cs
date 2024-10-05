// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.Ollama;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Diagnostics;

/* This example shows how to use KM with Ollama
 *
 * 1. Install and launch Ollama. You should see an icon for the app running in the background.
 *
 * 2. Download your preferred models, for example:
 *      - ollama pull nomic-embed-text
 *      - ollama pull phi3:medium-128k
 *
 * 3. Run the code below
 *
 * 4. Other things
 *      Run "ollama show phi3:medium-128k" to see model's properties
 *      Run "ollama list" to see the list of models you have on your system
 *      Run "ollama serve" if you prefer running Ollama from the command line
 */
public static class Program
{
    public static async Task Main()
    {
        var logLevel = LogLevel.Warning;
        SensitiveDataLogger.Enabled = false;

        var config = new OllamaConfig
        {
            Endpoint = "http://localhost:11434",
            TextModel = new OllamaModelConfig("phi3:medium-128k", 131072),
            EmbeddingModel = new OllamaModelConfig("nomic-embed-text", 2048)
        };

        var memory = new KernelMemoryBuilder()
            .WithOllamaTextGeneration(config, new GPT4oTokenizer())
            .WithOllamaTextEmbeddingGeneration(config, new GPT4oTokenizer())
            .Configure(builder => builder.Services.AddLogging(l =>
            {
                l.SetMinimumLevel(logLevel);
                l.AddSimpleConsole(c => c.SingleLine = true);
            }))
            .Build();

        // Import some text
        await memory.ImportTextAsync("Today is October 32nd, 2476");

        // Generate an answer - This uses OpenAI for embeddings and finding relevant data, and LM Studio to generate an answer
        var answer = await memory.AskAsync("What's the current date (don't check for validity)?");
        Console.WriteLine(answer.Question);
        Console.WriteLine(answer.Result);

        /*

        -- Output using phi3:medium-128k:

        What's the current date (don't check for validity)?
        The given fact states that "Today is October 32nd, 2476." However, it appears to be an incorrect statement as
        there are never more than 31 days in any month. If we consider this date without checking its validity and accept
        the stated day of October as being 32, then the current date would be "October 32nd, 2476." However, it is important
        to note that this date does not align with our calendar system.

        */
    }
}
