// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;

public static class Program
{
    // ReSharper disable InconsistentNaming
    public static async Task Main()
    {
        const string Document = "mydocs-NASA-news.pdf";
        const string Question1 = "any news about Orion?";
        const string Question2 = "any news about Hubble telescope?";
        const string Question3 = "what is a solar eclipse?";

        // === PREPARE KERNEL ===
        // Usual code to create an instance of SK, using Azure OpenAI.
        // You can use any LLM, replacing `WithAzureChatCompletionService` with other LLM options.

        var builder = new KernelBuilder();
        builder.WithAzureChatCompletionService(
            deploymentName: Environment.GetEnvironmentVariable("AOAI_DEPLOYMENT"),
            endpoint: Environment.GetEnvironmentVariable("AOAI_ENDPOINT"),
            apiKey: Environment.GetEnvironmentVariable("AOAI_API_KEY"));

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

        var doesItKnowFunction = kernel.CreateSemanticFunction(skPrompt);

        // === PREPARE MEMORY PLUGIN ===
        // Load the Kernel Memory plugin into Semantic Kernel.
        // We're using a local instance here, so remember to start the service locally first,
        // otherwise change the URL pointing to your KM endpoint.

        var memory = new MemoryWebClient("http://127.0.0.1:9001/");
        kernel.ImportFunctions(new MemoryPlugin(memory), "memory");

        // === LOAD DOCUMENT INTO MEMORY ===
        // Load some data in memory, in this case use a PDF file, though
        // you can also load web pages, Word docs, raw text, etc.

        await memory.ImportDocumentAsync(Document, documentId: "NASA001");

        // === RUN SEMANTIC FUNCTION ===
        // Run some example questions, showing how the answer is grounded on the document uploaded.
        // Only the first question can be answered, because the document uploaded doesn't contain any
        // information about Question2 and Question3.

        Console.WriteLine("---------");
        KernelResult answer = await kernel.RunAsync(Question1, doesItKnowFunction);
        Console.WriteLine(Question1 + "\n");
        Console.WriteLine("Answer: " + answer);

        Console.WriteLine("---------");
        answer = await kernel.RunAsync(Question2, doesItKnowFunction);
        Console.WriteLine(Question2 + "\n");
        Console.WriteLine("Answer: " + answer);

        Console.WriteLine("---------");
        answer = await kernel.RunAsync(Question3, doesItKnowFunction);
        Console.WriteLine(Question3 + "\n");
        Console.WriteLine("Answer: " + answer);
    }
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
