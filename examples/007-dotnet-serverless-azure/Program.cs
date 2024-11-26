// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Safety.AzureAIContentSafety;

/// <summary>
/// This example uses all and only Azure services
///
/// - Azure Blobs: used to store files.
/// - Azure AI Document Intelligence: used to extract text from images.
/// - Azure OpenAI: used to index data with embeddings and to generate answers.
/// - Azure AI Search: used to store embeddings and chunks of text.
/// - Azure Content Safety: validate LLM output to avoid unsafe content.
/// </summary>
public static class Program
{
    private static MemoryServerless? s_memory;
    private const string IndexName = "example006";

    public static async Task Main()
    {
        var memoryConfiguration = new KernelMemoryConfig();

        var azureAIContentSafetyConfig = new AzureAIContentSafetyConfig();
        var azureAIDocIntelConfig = new AzureAIDocIntelConfig();
        var azureAISearchConfig = new AzureAISearchConfig();
        var azureBlobConfig = new AzureBlobsConfig();
        var azureOpenAIEmbeddingConfig = new AzureOpenAIConfig();
        var azureOpenAITextConfig = new AzureOpenAIConfig();

        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.development.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build()
            .BindSection("KernelMemory", memoryConfiguration)
            .BindSection("KernelMemory:Services:AzureAIContentSafety", azureAIContentSafetyConfig)
            .BindSection("KernelMemory:Services:AzureAIDocIntel", azureAIDocIntelConfig)
            .BindSection("KernelMemory:Services:AzureAISearch", azureAISearchConfig)
            .BindSection("KernelMemory:Services:AzureBlobs", azureBlobConfig)
            .BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAIEmbeddingConfig)
            .BindSection("KernelMemory:Services:AzureOpenAIText", azureOpenAITextConfig);

        var builder = new KernelMemoryBuilder()
            .WithAzureBlobsDocumentStorage(azureBlobConfig)
            .WithAzureAIDocIntel(azureAIDocIntelConfig)
            .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig)
            .WithAzureOpenAITextGeneration(azureOpenAITextConfig)
            .WithAzureAISearchMemoryDb(azureAISearchConfig)
            .WithAzureAIContentSafetyModeration(azureAIContentSafetyConfig)
            .Configure(builder => builder.Services.AddLogging(l =>
            {
                l.SetMinimumLevel(LogLevel.Warning);
                l.AddSimpleConsole(c => c.SingleLine = true);
            }));

        s_memory = builder.Build<MemoryServerless>();

        await StoreWebPage();
        await StoreImage();

        // Test 1
        var question = "What's Kernel Memory?";
        Console.WriteLine($"Question: {question}");
        var answer = await s_memory.AskAsync(question, minRelevance: 0.5, index: IndexName);
        Console.WriteLine($"Answer: {answer.Result}\n\n");

        // Test 2
        question = "Which conference is Microsoft sponsoring?";
        Console.WriteLine($"Question: {question}");
        answer = await s_memory.AskAsync(question, minRelevance: 0.5, index: IndexName);
        Console.WriteLine($"Answer: {answer.Result}\n\n");
    }

    // Downloading web pages
    private static async Task StoreWebPage()
    {
        const string DocId = "webPage1";
        if (!await s_memory!.IsDocumentReadyAsync(DocId, index: IndexName))
        {
            Console.WriteLine("Uploading https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md");
            await s_memory.ImportWebPageAsync("https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md", index: IndexName, documentId: DocId);
        }
        else
        {
            Console.WriteLine($"{DocId} already uploaded.");
        }
    }

    // Extract memory from images (OCR required)
    private static async Task StoreImage()
    {
        const string DocId = "img001";
        if (!await s_memory!.IsDocumentReadyAsync(DocId, index: IndexName))
        {
            Console.WriteLine("Uploading Image file with a news about a conference sponsored by Microsoft");
            await s_memory.ImportDocumentAsync(new Document(DocId).AddFiles(["file6-ANWC-image.jpg"]), index: IndexName);
        }
        else
        {
            Console.WriteLine($"{DocId} already uploaded.");
        }
    }
}
