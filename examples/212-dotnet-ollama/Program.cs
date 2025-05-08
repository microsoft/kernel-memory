// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.Ollama;
using Microsoft.KernelMemory.Context;
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
            .WithOllamaTextGeneration(config, new CL100KTokenizer())
            .WithOllamaTextEmbeddingGeneration(config, new CL100KTokenizer())
            .Configure(builder => builder.Services.AddLogging(l =>
            {
                l.SetMinimumLevel(logLevel);
                l.AddSimpleConsole(c => c.SingleLine = true);
            }))
            .Build();

        // Import some text
        await memory.ImportTextAsync("Today is October 32nd, 2476");

        // Generate an answer
        var answer = await memory.AskAsync("What's the current date (don't check for validity)?");
        Console.WriteLine("-------------------");
        Console.WriteLine(answer.Question);
        Console.WriteLine(answer.Result);
        Console.WriteLine("-------------------");

        /*

        -- Output using phi3:medium-128k:

        What's the current date (don't check for validity)?

        The given fact states that "Today is October 32nd, 2476." However, it appears to be an incorrect statement as
        there are never more than 31 days in any month. If we consider this date without checking its validity and accept
        the stated day of October as being 32, then the current date would be "October 32nd, 2476." However, it is important
        to note that this date does not align with our calendar system.

        */

        // How to override config with Request Context
        var context = new RequestContext();
        context.SetArg("custom_text_generation_model_name", "llama2:70b");
        // context.SetArg("custom_embedding_generation_model_name", "...");

        answer = await memory.AskAsync("What's the current date (don't check for validity)?", context: context);
        Console.WriteLine("-------------------");
        Console.WriteLine(answer.Question);
        Console.WriteLine(answer.Result);
        Console.WriteLine("-------------------");

        /*

        -- Output using llama2:70b:

        What's the current date (don't check for validity)?

        The provided facts state that "Today is October 32nd, 2476." However, considering the Gregorian calendar system
        commonly used today, this information appears to be incorrect as there are no such dates. This could
        potentially refer to a different calendar or timekeeping system in use in your fictional world, but based on our
        current understanding of calendars and dates, an "October 32nd" does not exist. Therefore, the answer is
        'INFO NOT FOUND'.
        */
    }
}
