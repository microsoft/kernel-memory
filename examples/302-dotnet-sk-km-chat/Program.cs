// Copyright (c) Microsoft. All rights reserved.

using System.Security.Cryptography;
using System.Text;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

internal sealed class Program
{
    private static readonly List<string> s_documentation =
    [
        "https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md",
        "https://microsoft.github.io/kernel-memory/quickstart",
        "https://microsoft.github.io/kernel-memory/quickstart/configuration",
        "https://microsoft.github.io/kernel-memory/quickstart/start-service",
        "https://microsoft.github.io/kernel-memory/quickstart/python",
        "https://microsoft.github.io/kernel-memory/quickstart/csharp",
        "https://microsoft.github.io/kernel-memory/quickstart/java",
        "https://microsoft.github.io/kernel-memory/quickstart/javascript",
        "https://microsoft.github.io/kernel-memory/quickstart/bash",
        "https://microsoft.github.io/kernel-memory/service",
        "https://microsoft.github.io/kernel-memory/service/architecture",
        "https://microsoft.github.io/kernel-memory/serverless",
        "https://microsoft.github.io/kernel-memory/security/filters",
        "https://microsoft.github.io/kernel-memory/how-to/custom-partitioning",
        "https://microsoft.github.io/kernel-memory/concepts/indexes",
        "https://microsoft.github.io/kernel-memory/concepts/document",
        "https://microsoft.github.io/kernel-memory/concepts/memory",
        "https://microsoft.github.io/kernel-memory/concepts/tag",
        "https://microsoft.github.io/kernel-memory/concepts/llm",
        "https://microsoft.github.io/kernel-memory/concepts/embedding",
        "https://microsoft.github.io/kernel-memory/concepts/cosine-similarity",
        "https://microsoft.github.io/kernel-memory/faq",
        "https://raw.githubusercontent.com/microsoft/semantic-kernel/main/README.md",
        "https://raw.githubusercontent.com/microsoft/semantic-kernel/main/dotnet/README.md",
        "https://raw.githubusercontent.com/microsoft/semantic-kernel/main/python/README.md",
        "https://raw.githubusercontent.com/microsoft/semantic-kernel/main/java/README.md",
        "https://learn.microsoft.com/en-us/semantic-kernel/overview/",
        "https://learn.microsoft.com/en-us/semantic-kernel/get-started/quick-start-guide",
        "https://learn.microsoft.com/en-us/semantic-kernel/agents/",
    ];

    internal static async Task Main()
    {
        var openAIApiKey = Environment.GetEnvironmentVariable("OPENAI_APIKEY") ?? throw new ConfigurationException("OPENAI_APIKEY env var not found");

        Kernel kernel = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(modelId: "gpt-4", apiKey: openAIApiKey)
            .Build();

        // Memory instance with persistent storage on disk
        IKernelMemory memory = new KernelMemoryBuilder()
            .WithOpenAIDefaults(openAIApiKey)
            .WithSimpleVectorDb(SimpleVectorDbConfig.Persistent)
            .WithSimpleFileStorage(SimpleFileStorageConfig.Persistent)
            .Build<MemoryServerless>();

        // Memorize some data
        Console.WriteLine("# Saving documentation into kernel memory...");
        await MemorizeDocuments(memory, s_documentation);

        // Infinite chat loop
        Console.WriteLine("# Starting chat...");
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        await ChatLoop(chatService, memory);
    }

    private static async Task ChatLoop(IChatCompletionService chatService, IKernelMemory memory)
    {
        // Chat setup
        var systemPrompt = """
                           You are a helpful assistant replying to user questions using information from your memory.
                           Reply very briefly and concisely, get to the point immediately. Don't provide long explanations unless necessary.
                           Sometimes you don't have relevant memories so you reply saying you don't know, don't have the information.
                           The topic of the conversation is Kernel Memory (KM) and Semantic Kernel (SK).
                           """;

        var chatHistory = new ChatHistory(systemPrompt);

        // Start the chat
        var assistantMessage = "Hello, how can I help?";
        Console.WriteLine($"Copilot> {assistantMessage}\n");
        chatHistory.AddAssistantMessage(assistantMessage);

        // Infinite chat loop
        var reply = new StringBuilder();

        while (true)
        {
            // Get user message (retry if the user enters an empty string)
            Console.Write("You> ");
            var userMessage = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(userMessage)) { continue; }
            else { chatHistory.AddUserMessage(userMessage); }

            // Recall relevant information from memory
            var longTermMemory = await GetLongTermMemory(memory, userMessage);
            // Console.WriteLine("-------------------------- recall from memory\n{longTermMemory}\n--------------------------");

            // Inject the memory recall in the initial system message
            chatHistory[0].Content = $"{systemPrompt}\n\nLong term memory:\n{longTermMemory}";

            // Generate the next chat message, stream the response
            Console.Write("\nCopilot> ");
            reply.Clear();
            await foreach (StreamingChatMessageContent stream in chatService.GetStreamingChatMessageContentsAsync(chatHistory))
            {
                Console.Write(stream.Content);
                reply.Append(stream.Content);
            }

            chatHistory.AddAssistantMessage(reply.ToString());
            Console.WriteLine("\n");
        }
    }

    private static async Task<string> GetLongTermMemory(IKernelMemory memory, string query, bool asChunks = true)
    {
        if (asChunks)
        {
            // Fetch raw chunks, using KM indexes. More tokens to process with the chat history, but only one LLM request.
            SearchResult memories = await memory.SearchAsync(query, limit: 10);
            return memories.Results.SelectMany(m => m.Partitions).Aggregate("", (sum, chunk) => sum + chunk.Text + "\n").Trim();
        }

        // Use KM to generate an answer. Fewer tokens, but one extra LLM request.
        MemoryAnswer answer = await memory.AskAsync(query);
        return answer.Result.Trim();
    }

    private static async Task MemorizeDocuments(IKernelMemory memory, List<string> pages)
    {
        await memory.ImportTextAsync("We can talk about Semantic Kernel and Kernel Memory, you can ask any questions, I will try to reply using information from public documentation in Github", documentId: "help");
        foreach (var url in pages)
        {
            var id = GetUrlId(url);
            // Check if the page is already in memory, to avoid importing twice
            if (!await memory.IsDocumentReadyAsync(id))
            {
                await memory.ImportWebPageAsync(url, documentId: id);
            }
        }
    }

    private static string GetUrlId(string url)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url))).ToUpperInvariant();
    }
}

/* Example output:

   # Saving documentation into kernel memory...
   # Starting chat...
   Copilot> Hello, how can I help?

   You> what can I ask?

   Copilot> You can ask queries related to Semantic Kernel and Kernel Memory based on Github public documentation.
            Examples of questions could be about importing web page content, targeting specific documents with questions,
            how to use Kernel Memory from the command line, or about KM's security filters.

   You> ok tell me more

   Copilot> Kernel Memory (KM) is a Long Term Memory solution for your applications, designed to function similarly to
            human memory, storing and retrieving information. It works by uploading data in a packaged Document form.
            Whether it's a web page URL, files, or strings, these are stored as Documents with unique IDs to avoid duplications.
            KM leverages Large Language Models (LLM), such as GPT-4 and Ada-2 from AI providers, to extract meaning from documents
            and generate sentences when asking questions.

            Semantic Kernel (SK) constitutes the core components that orchestrate your code with AI. It consists of Plugins,
            which are used to manipulate the real world, and Prompts that help generate correct responses. Planners,
            another key component, are Prompts that help generate plans to complete tasks.

            You can interact with KM using command line tools, make specific queries to documents, and ensure
            secure operations using KM's Security Filters.

   You> could you provide some examples?

   Copilot> Sure, here are some examples of interacting with Kernel Memory via command line:

            1. To upload a document:
            ```bash
            cd tools
            ./upload-file.sh -f README.md -i doc01 -s http://127.0.0.1:9001
            ```
            Or using Curl:
            ```bash
            curl -F 'file1=@"README.md"' -F 'documentId="doc01"' http://127.0.0.1:9001/upload
            ```

            2. To ask a question:
            ```bash
            cd tools
            ./ask.sh -q "Can I use KM from command line?" -s http://127.0.0.1:9001
            ```
            Or using Curl:
            ```bash
            curl -d'{"question":"Can I use KM from command line?"}' -H 'Content-Type: application/json' http://127.0.0.1:9001/ask
            ```

            These examples demonstrate how to upload a document to KM and how to ask a question using KM's API.
            Remember to replace `README.md`, `doc01` and `Can I use KM from command line?` with your file name, document id, and your question.


   You> and sk?

   Copilot> When working with Semantic Kernel (SK), one of the core concepts is "prompts". These are used to
            generate responses from AI models. Here's an example of how you might create a prompt function:

            ```python
            from semantic_kernel import Kernel

            # Define your prompt function
            def greeting_prompt(name):
              return f"Hello, {name}. How can I assist you today?"

            # Initialize the kernel
            kernel = Kernel.get_instance()

            # Register your prompt function
            kernel.register_prompt_fn(greeting_prompt)
            ```

            In this Python example, a function `greeting_prompt` is defined which takes one argument `name`. This
            function is then registered in the SK system using the `register_prompt_fn` method. Now, when sending
            inputs to the model, it will generate responses as if it was saying "Hello, [name]. How can I assist you today?".
            This helps guide the message generated by AI in a specific direction.

            Remember, this is a basic example. SK's powerful features let you run complex prompts, use different
            AI services, manage plug-ins for real-world interaction, and more.
*/
