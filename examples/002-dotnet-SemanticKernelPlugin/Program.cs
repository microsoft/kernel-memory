// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;

public static class Program
{
    // ReSharper disable InconsistentNaming
    public static async Task Main()
    {
        const string DocFilename = "mydocs-NASA-news.pdf";
        const string Question1 = "any news about Orion?";
        const string Question2 = "any news about Hubble telescope?";
        const string Question3 = "what is a solar eclipse?";

        // === PREPARE KERNEL ===
        // Usual code to create an instance of SK, using Azure OpenAI.
        // You can use any LLM, replacing `WithAzureChatCompletionService` with other LLM options.

        var builder = Kernel.CreateBuilder();
        builder
            // For OpenAI:
            //.AddOpenAIChatCompletion(
            //     modelId: "gpt-3.5-turbo",
            //     apiKey: EnvVar("OPENAI_API_KEY"))
            // Azure OpenAI:
            .AddAzureOpenAIChatCompletion(
                deploymentName: EnvVar("AOAI_DEPLOYMENT_TEXT"),
                modelId: EnvVar("AOAI_DEPLOYMENT_TEXT"),
                endpoint: EnvVar("AOAI_ENDPOINT"),
                apiKey: EnvVar("AOAI_API_KEY"));

        var kernel = builder.Build();

        // === PREPARE A SIMPLE SEMANTIC FUNCTION
        // A simple prompt showing how you can leverage the memory inside prompts and semantic functions.
        // See how "memory.ask" is used to pass the user question. At runtime the block is replaced with the
        // answer provided by the memory service.

        var skPrompt = """
                       Question to Kernel Memory: {{$input}}

                       Kernel Memory Answer: {{memory.ask $input}}

                       If the answer is empty say 'I don't know' otherwise reply with a preview of the answer, truncated to 15 words.
                       """;

        var myFunction = kernel.CreateFunctionFromPrompt(skPrompt);

        // === PREPARE MEMORY PLUGIN ===
        // Load the Kernel Memory plugin into Semantic Kernel.
        // We're using a local instance here, so remember to start the service locally first,
        // otherwise change the URL pointing to your KM endpoint.

        var memoryConnector = GetMemoryConnector();
        var memoryPlugin = kernel.ImportPluginFromObject(new MemoryPlugin(memoryConnector, waitForIngestionToComplete: true), "memory");

        // === LOAD DOCUMENT INTO MEMORY ===
        // Load some data in memory, in this case use a PDF file, though
        // you can also load web pages, Word docs, raw text, etc.

        // You can use either the plugin or the connector, the result is the same
        // await memoryConnector.ImportDocumentAsync(filePath: DocFilename, documentId: "NASA001");
        var context = new KernelArguments
        {
            [MemoryPlugin.FilePathParam] = DocFilename,
            [MemoryPlugin.DocumentIdParam] = "NASA001"
        };
        await memoryPlugin["SaveFile"].InvokeAsync(kernel, context);

        // === RUN SEMANTIC FUNCTION ===
        // Run some example questions, showing how the answer is grounded on the document uploaded.
        // Only the first question can be answered, because the document uploaded doesn't contain any
        // information about Question2 and Question3.

        Console.WriteLine("---------");
        Console.WriteLine(Question1 + " (expected: some answer using the PDF provided)\n");
        var answer = await myFunction.InvokeAsync(kernel, Question1);
        Console.WriteLine("Answer: " + answer);

        Console.WriteLine("---------");
        Console.WriteLine(Question2 + " (expected answer: \"I don't know\")\n");
        answer = await myFunction.InvokeAsync(kernel, Question2);
        Console.WriteLine("Answer: " + answer);

        Console.WriteLine("---------");
        Console.WriteLine(Question3 + " (expected answer: \"I don't know\")\n");
        answer = await myFunction.InvokeAsync(kernel, Question3);
        Console.WriteLine("Answer: " + answer);
    }

    /* ===== OUTPUT =====

    ---------
    any news about Orion?

    Answer: Yes, NASA has invited media to see the new test version of the Orion spacecraft...
    ---------
    any news about Hubble telescope?

    Answer: I don't know.
    ---------
    what is a solar eclipse?

    Answer: I don't know.

    */

    private static IKernelMemory GetMemoryConnector(bool serverless = false)
    {
        if (!serverless)
        {
            return new MemoryWebClient("http://127.0.0.1:9001/", Environment.GetEnvironmentVariable("MEMORY_API_KEY"));
        }

        Console.WriteLine("This code is intentionally disabled.");
        Console.WriteLine("To test the plugin with Serverless memory:");
        Console.WriteLine("* Add a project reference to CoreLib");
        Console.WriteLine("* Uncomment/edit the code in " + nameof(GetMemoryConnector));
        Environment.Exit(-1);
        return null;

        // return new KernelMemoryBuilder()
        //     .WithAzureOpenAIEmbeddingGeneration(new AzureOpenAIConfig
        //     {
        //         APIType = AzureOpenAIConfig.APITypes.EmbeddingGeneration,
        //         Endpoint = EnvVar("AOAI_ENDPOINT"),
        //         Deployment = EnvVar("AOAI_DEPLOYMENT_EMBEDDING"),
        //         Auth = AzureOpenAIConfig.AuthTypes.APIKey,
        //         APIKey = EnvVar("AOAI_API_KEY"),
        //     })
        //     .WithAzureOpenAITextGeneration(new AzureOpenAIConfig
        //     {
        //         APIType = AzureOpenAIConfig.APITypes.ChatCompletion,
        //         Endpoint = EnvVar("AOAI_ENDPOINT"),
        //         Deployment = EnvVar("AOAI_DEPLOYMENT_TEXT"),
        //         Auth = AzureOpenAIConfig.AuthTypes.APIKey,
        //         APIKey = EnvVar("AOAI_API_KEY"),
        //     })
        //     .Build<MemoryServerless>();
    }

    private static string EnvVar(string name)
    {
        return Environment.GetEnvironmentVariable(name)
               ?? throw new ArgumentException($"Env var {name} not set");
    }
}
