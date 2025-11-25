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

    // Use these booleans in case you don't want to use these Azure Services
    private const bool UseAzureAIDocIntelligence = true;
    private const bool UseAzureAIContentSafety = true;

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
            .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig)
            .WithAzureOpenAITextGeneration(azureOpenAITextConfig)
            .WithAzureAISearchMemoryDb(azureAISearchConfig)
            // .WithAzureAIDocIntel(azureAIDocIntelConfig) // see below
            // .WithAzureAIContentSafetyModeration(azureAIContentSafetyConfig) // see below
            .Configure(builder => builder.Services.AddLogging(l =>
            {
                l.SetMinimumLevel(LogLevel.Error);
                l.AddSimpleConsole(c => c.SingleLine = true);
            }));

        // We split this builder code out in case you don't have these Azure services
        if (UseAzureAIDocIntelligence) { builder.WithAzureAIContentSafetyModeration(azureAIContentSafetyConfig); }

        if (UseAzureAIContentSafety) { builder.WithAzureAIDocIntel(azureAIDocIntelConfig); }

        s_memory = builder.Build<MemoryServerless>();

        // ====== Store some data ======

        await StoreWebPageAsync(); // Works with Azure AI Search and Azure OpenAI
        await StoreImageAsync(); // Works only if Azure AI Document Intelligence is used

        // ====== Answer some questions ======

        // When using hybrid search, relevance is much lower than cosine similarity
        var minRelevance = azureAISearchConfig.UseHybridSearch ? 0 : 0.5;

        // Test 1 (answer from the web page)
        var question = "What's Kernel Memory?";
        Console.WriteLine($"Question: {question}");
        var answer = await s_memory.AskAsync(question, minRelevance: minRelevance, index: IndexName);
        Console.WriteLine($"Answer: {answer.Result}\n\n");

        // Test 2 (requires Azure AI Document Intelligence to have parsed the image)
        question = "Which conference is Microsoft sponsoring?";
        Console.WriteLine($"Question: {question}");
        answer = await s_memory.AskAsync(question, minRelevance: minRelevance, index: IndexName);
        Console.WriteLine($"Answer: {answer.Result}\n\n");
    }

    // Downloading web pages
    private static async Task StoreWebPageAsync()
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

    // Extract memory from images (requires Azure AI Document Intelligence)
    private static async Task StoreImageAsync()
    {
        if (!UseAzureAIDocIntelligence) { return; }

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
