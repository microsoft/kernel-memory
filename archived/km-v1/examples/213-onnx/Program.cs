// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;

/* This example shows how to use KM with Ollama
 *
 * 1. Download phi4 model from https://huggingface.co/microsoft/phi-4-onnx
 *
 * 2. Edit appsettings.json (or appsettings.Development.json) and set the model path.
 *
 * 3. Run the code
 */
public static class Program
{
    public static async Task Main()
    {
        var onnxCfg = new OnnxConfig();
        var azureOpenAIEmbeddingConfig = new AzureOpenAIConfig();

        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.development.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build()
            .BindSection("KernelMemory:Services:Onnx", onnxCfg)
            .BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAIEmbeddingConfig);

        var memory = new KernelMemoryBuilder()
            .WithOnnxTextGeneration(onnxCfg)
            .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig)
            .Configure(builder => builder.Services.AddLogging(l =>
            {
                l.SetMinimumLevel(LogLevel.Warning);
                l.AddSimpleConsole(c => c.SingleLine = true);
            }))
            .Build();

        // Import some text
        await memory.ImportTextAsync("Yesterday was October 21st, 2476");
        await memory.ImportTextAsync("Tomorrow will be October 23rd, 2476");

        // Generate an answer
        var answer = await memory.AskAsync("What's the current date?");
        Console.WriteLine(answer.Result);

        /*

        -- Output using phi-4-onnx:

        Based on the provided information, if yesterday was October 21st, 2476, then today is October 22nd, 2476.
        */
    }
}
