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
                           The topic of the conversation is Kernel Memory (KM) and Semantic Kernel (SK).
                           Sometimes you don't have relevant memories so you reply saying you don't know, don't have the information.
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
            MemoryAnswer recall = await memory.AskAsync(userMessage);
            Console.WriteLine("--- recall from memory ---");
            Console.WriteLine(recall.Result.Trim());
            Console.WriteLine("--------------------------");

            // Inject the memory recall in the initial system message
            chatHistory[0].Content = $"{systemPrompt}\n\nLong term memory:\n{recall.Result}";

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

    private static async Task MemorizeDocuments(IKernelMemory memory, List<string> pages)
    {
        await memory.ImportTextAsync("We can talk about Semantic Kernel and Kernel Memory, you can ask any questions, I will try to reply using information from KM public documentation in Github", documentId: "help");
        foreach (var url in pages)
        {
            var id = GetUrlId(url);
            // Check if the page is already in memory, to avoid importing twice
            if (!await memory.IsDocumentReadyAsync(id))
            {
                await memory.ImportWebPageAsync(url, documentId: GetUrlId(url));
            }
        }
    }

    private static string GetUrlId(string url)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url))).ToUpperInvariant();
    }
}
