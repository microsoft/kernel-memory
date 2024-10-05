// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.AzureOpenAI;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.DataFormats;
using Microsoft.KernelMemory.DataFormats.AzureAIDocIntel;
using Microsoft.KernelMemory.DataFormats.Image;
using Microsoft.KernelMemory.DataFormats.Office;
using Microsoft.KernelMemory.DataFormats.Pdf;
using Microsoft.KernelMemory.DataFormats.Text;
using Microsoft.KernelMemory.DataFormats.WebPages;
using Microsoft.KernelMemory.DocumentStorage.AzureBlobs;
using Microsoft.KernelMemory.Handlers;
using Microsoft.KernelMemory.MemoryDb.AzureAISearch;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.KernelMemory.Prompts;
using Microsoft.KernelMemory.Search;

/// <summary>
/// This example shows how to create an instance of Kernel Memory without using Kernel Memory builder
/// and without .NET Dependency Injection, in other words how to compose all classes manually.
///
/// The example focuses on MemoryServerless, but it works as well for MemoryService with some extra code.
///
/// This is just for the purpose of demonstration.
/// </summary>
[Experimental("KMEXP00")]
#pragma warning disable CA1849 // No need to use async code
public static class Program
{
    public static async Task Main()
    {
        // Configurations
        var kernelMemoryConfig = new KernelMemoryConfig();
        var searchClientConfig = new SearchClientConfig();
        var azureBlobsConfig = new AzureBlobsConfig();
        var azureAISearchConfig = new AzureAISearchConfig();
        var azureOpenAIEmbeddingConfig = new AzureOpenAIConfig();
        var azureOpenAITextConfig = new AzureOpenAIConfig();
        var azureAIDocIntelConfig = new AzureAIDocIntelConfig();
        var textPartitioningOptions = new TextPartitioningOptions();
        var msExcelDecoderConfig = new MsExcelDecoderConfig();
        var msPowerPointDecoderConfig = new MsPowerPointDecoderConfig();

        // Use ASP.NET to load settings == optional ========================
        WebApplicationBuilder appBuilder = WebApplication.CreateBuilder();
        appBuilder.Configuration
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.development.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables();
        var app = appBuilder.Build();
        app.Configuration
            .BindSection("KernelMemory", kernelMemoryConfig)
            .BindSection("KernelMemory:DataIngestion:TextPartitioning", textPartitioningOptions)
            .BindSection("KernelMemory:Retrieval:SearchClient", searchClientConfig)
            .BindSection("KernelMemory:Services:AzureBlobs", azureBlobsConfig)
            .BindSection("KernelMemory:Services:AzureAISearch", azureAISearchConfig)
            .BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAIEmbeddingConfig)
            .BindSection("KernelMemory:Services:AzureOpenAIText", azureOpenAITextConfig)
            .BindSection("KernelMemory:Services:AzureAIDocIntel", azureAIDocIntelConfig);
        // =================================================================

        // Logger
        LoggerFactory? loggerFactory = null; // Alternative: app.Services.GetService<ILoggerFactory>();

        // Generic dependencies
        var mimeTypeDetection = new MimeTypesDetection();
        var promptProvider = new EmbeddedPromptProvider();

        // AI dependencies
        var tokenizer = new GPT4oTokenizer();
        var embeddingGeneratorHttpClient = new HttpClient();
        var embeddingGenerator = new AzureOpenAITextEmbeddingGenerator(azureOpenAIEmbeddingConfig, tokenizer, loggerFactory, embeddingGeneratorHttpClient);
        var textGeneratorHttpClient = new HttpClient();
        var textGenerator = new AzureOpenAITextGenerator(azureOpenAITextConfig, tokenizer, loggerFactory, textGeneratorHttpClient);

        // Storage
        var documentStorage = new AzureBlobsStorage(azureBlobsConfig, mimeTypeDetection, loggerFactory);
        var memoryDb = new AzureAISearchMemory(azureAISearchConfig, embeddingGenerator, loggerFactory);

        // Ingestion pipeline orchestration
        var memoryDbs = new List<IMemoryDb> { memoryDb };
        var embeddingGenerators = new List<ITextEmbeddingGenerator> { embeddingGenerator };
        var orchestrator = new InProcessPipelineOrchestrator(documentStorage, embeddingGenerators, memoryDbs, textGenerator, mimeTypeDetection, null, kernelMemoryConfig, loggerFactory);

        // Ingestion handlers' dependencies
        var webScraperHttpClient = new HttpClient();
        var webScraper = new WebScraper(webScraperHttpClient);
        var ocrEngine = new AzureAIDocIntelEngine(azureAIDocIntelConfig, loggerFactory);
        var decoders = new List<IContentDecoder>
        {
            new TextDecoder(loggerFactory),
            new HtmlDecoder(loggerFactory),
            new MarkDownDecoder(loggerFactory),
            new PdfDecoder(loggerFactory),
            new MsWordDecoder(loggerFactory),
            new MsExcelDecoder(msExcelDecoderConfig, loggerFactory),
            new MsPowerPointDecoder(msPowerPointDecoderConfig, loggerFactory),
            new ImageDecoder(ocrEngine, loggerFactory),
        };

        // Ingestion handlers
        orchestrator.AddHandler(new TextExtractionHandler("extract", orchestrator, decoders, webScraper, loggerFactory));
        orchestrator.AddHandler(new TextPartitioningHandler("partition", orchestrator, textPartitioningOptions, loggerFactory));
        orchestrator.AddHandler(new GenerateEmbeddingsHandler("gen_embeddings", orchestrator, loggerFactory));
        orchestrator.AddHandler(new SaveRecordsHandler("save_records", orchestrator, kernelMemoryConfig, loggerFactory));
        orchestrator.AddHandler(new SummarizationHandler("summarize", orchestrator, promptProvider, loggerFactory));
        orchestrator.AddHandler(new DeleteGeneratedFilesHandler("delete_generated_files", documentStorage, loggerFactory));
        orchestrator.AddHandler(new DeleteIndexHandler("private_delete_index", documentStorage, memoryDbs, loggerFactory));
        orchestrator.AddHandler(new DeleteDocumentHandler("private_delete_document", documentStorage, memoryDbs, loggerFactory));

        // Create memory instance
        var searchClient = new SearchClient(memoryDb, textGenerator, searchClientConfig, promptProvider, loggerFactory);
        var memory = new MemoryServerless(orchestrator, searchClient, kernelMemoryConfig);

        // End-to-end test
        await memory.ImportTextAsync("I'm waiting for Godot", documentId: "tg01");
        Console.WriteLine(await memory.AskAsync("Who am I waiting for?"));
    }
}
